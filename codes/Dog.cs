using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DogShelter.Core;
using DogShelter.Models;
using SkyNet;

namespace DogShelter.codes
{
    public class Dog : WebPage
    {
        private sealed class View
        {
            public string Title { get; set; } = string.Empty;
            public string Desc { get; set; } = string.Empty;
            public string Main { get; set; } = string.Empty;
            public string Nav { get; set; } = string.Empty;
        }

        private string Q(string key, int max = 80)
        {
            return Ui.Clip(QueryValue(key), max);
        }

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            View v = await Render(site);
            HtmlDoc.SetTitle((v.Title != string.Empty ? v.Title + " | " : string.Empty) + site.Settings.Name);
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("description", v.Desc != string.Empty ? v.Desc : site.Settings.Tagline);
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText
                .Replace("{plhd_name}", Ui.Enc(site.Settings.Name))
                .Replace("{plhd_town}", Ui.Enc(site.Settings.Town))
                .Replace("{plhd_tagline}", Ui.Enc(site.Settings.Tagline))
                .Replace("{plhd_addr}", Ui.Enc(site.Settings.Address))
                .Replace("{plhd_phone}", Ui.Enc(site.Settings.Phone))
                .Replace("{plhd_tel}", Ui.Attr(Regex.Replace(site.Settings.Phone, "[^0-9+]", string.Empty)))
                .Replace("{plhd_email}", Ui.Enc(site.Settings.Email))
                .Replace("{plhd_charity}", Ui.Enc(site.Settings.Charity))
                .Replace("{plhd_today}", TodayLine(site))
                .Replace("{plhd_theme}", site.Settings.Theme == "dark" ? "dark" : "light")
                .Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--dd-accent:" + site.Settings.Accent : string.Empty)
                .Replace("{plhd_nav}", Nav("dd", v.Nav, false))
                .Replace("{plhd_drawer}", Nav("dd", v.Nav, true))
                .Replace("{plhd_main}", v.Main);
        }

        private static string Nav(string p, string on, bool drawer)
        {
            (string Href, string Key, string Name)[] links = { ("Dogs", "dogs", "Adopt a dog"), ("Adopt", "adopt", "How to adopt"), ("Foster", "foster", "Foster"), ("Volunteer", "volunteer", "Volunteer"), ("Events", "events", "Events"), ("Stories", "stories", "Happy tails"), ("Contact", "contact", "Contact") };
            StringBuilder sb = new StringBuilder();
            if (drawer)
            {
                sb.Append("<a href=\"Home\"" + (on == "home" ? " class=\"" + p + "-on\"" : string.Empty) + ">Home</a>");
            }
            foreach ((string href, string k, string n) in links)
            {
                sb.Append("<a href=\"" + href + "\"" + (k == on ? " class=\"" + p + "-on\" aria-current=\"page\"" : string.Empty) + ">" + Ui.Enc(n) + "</a>");
            }
            if (drawer)
            {
                sb.Append("<a href=\"Dogs?saved=1\">Saved dogs</a>");
            }
            return sb.ToString();
        }

        private static string TodayLine(SiteData site)
        {
            DateTime now = Store.Now(site);
            OpenDay? d = site.Settings.Hours.FirstOrDefault(h => h.Day == (int)now.DayOfWeek);
            TimeSpan? from = d != null && d.Open ? Store.Hm(d.Start) : null;
            TimeSpan? to = d != null && d.Open ? Store.Hm(d.End) : null;
            if (from != null && to != null && now.TimeOfDay >= from.Value && now.TimeOfDay < to.Value)
            {
                return "<i></i>Open for visits today until " + Ui.HmText(site, d!.End);
            }
            if (from != null && now.TimeOfDay < from.Value)
            {
                return "<i class=\"dd-shut\"></i>Visiting hours today from " + Ui.HmText(site, d!.Start);
            }
            for (int i = 1; i <= 7; i++)
            {
                DateTime n = now.Date.AddDays(i);
                OpenDay? o = site.Settings.Hours.FirstOrDefault(h => h.Day == (int)n.DayOfWeek);
                if (o != null && o.Open)
                {
                    return "<i class=\"dd-shut\"></i>Closed now &middot; open " + (i == 1 ? "tomorrow" : Ui.DayNames[(int)n.DayOfWeek]) + " from " + Ui.HmText(site, o.Start);
                }
            }
            return "<i class=\"dd-shut\"></i>Closed";
        }

        private static string HoursRows(SiteData site, string p)
        {
            int today = (int)Store.Today(site).DayOfWeek;
            StringBuilder sb = new StringBuilder();
            foreach (int d in new[] { 1, 2, 3, 4, 5, 6, 0 })
            {
                OpenDay? o = site.Settings.Hours.FirstOrDefault(h => h.Day == d);
                string t = o != null && o.Open ? Ui.HmText(site, o.Start) + " &ndash; " + Ui.HmText(site, o.End) : "Closed";
                sb.Append("<span class=\"" + p + "-hrow" + (d == today ? " " + p + "-now" : string.Empty) + "\"><span>" + Ui.DayNames[d] + "</span><span>" + t + "</span></span>");
            }
            return sb.ToString();
        }

        public async Task<ApiResponse> Subscribe()
        {
            ApiResponse response = new ApiResponse();
            string mail = Ui.Clip(GetDataValue("nlmail"), 120);
            if (!Ui.Email(mail))
            {
                response.SetElementContents("dd-e-nlmail", "Enter an email address like name@example.com.");
                return response;
            }
            bool added = await Store.AddSubscriber(mail);
            response.ExecuteScript("DogJs.subscribed('" + (added ? "Thanks. Watch your inbox for new dogs and events." : "You are already on the list.") + "');");
            return response;
        }

        public async Task<ApiResponse> Heart()
        {
            ApiResponse response = new ApiResponse();
            string id = Ui.Clip(GetDataValue("hid"), 20);
            bool on = GetDataValue("hon") == "1";
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Pet? d = site.Dogs.FirstOrDefault(x => x.Id == id && Ui.Public(x));
                if (d != null)
                {
                    d.Saves = Math.Max(0, d.Saves + (on ? 1 : -1));
                    await Store.SaveSite(site);
                }
            }
            return response;
        }

        private static string Facts(Pet d, DateTime today)
        {
            return "<span>" + Ui.SexName(d.Sex) + "</span><span>" + Ui.Age(d, today) + "</span><span>" + Ui.SizeName(d.Size) + "</span>";
        }

        private static (string Cls, string Text) Badge(SiteData site, Pet d, DateTime today)
        {
            if (d.Status == "hold")
            {
                return ("hold", "Adoption pending");
            }
            if (d.Bonded != string.Empty)
            {
                return ("pair", "Bonded pair");
            }
            if (d.Urgent)
            {
                return ("urgent", "Urgent");
            }
            if (d.Status == "foster")
            {
                return ("foster", "In foster");
            }
            if (d.Listed != null && d.Listed.Value.Date >= today.AddDays(-7))
            {
                return ("new", "New");
            }
            return (string.Empty, string.Empty);
        }

        private static int Taken(Event e)
        {
            return e.Rsvps.Sum(r => r.Guests);
        }

        private static string EvWhen(SiteData site, Event e)
        {
            return e.Date.ToString("dddd, MMMM d", Ui.Inv) + " &middot; " + Ui.HmText(site, e.Start) + " &ndash; " + Ui.HmText(site, e.End);
        }

        private static string EvTile(string p, Event e)
        {
            return "<span class=\"" + p + "-evd\"><b>" + e.Date.ToString("MMM", Ui.Inv).ToUpperInvariant() + "</b><i>" + e.Date.Day + "</i><small>" + e.Date.ToString("ddd", Ui.Inv) + "</small></span>";
        }

        private static string Good(string what, string v)
        {
            return v == "ask" ? string.Empty : "<span class=\"dd-gw" + (v == "no" ? " dd-no" : string.Empty) + "\">" + what + "</span>";
        }

        private static string Card(SiteData site, Pet d, DateTime today)
        {
            string href = "Dog?id=" + Uri.EscapeDataString(d.Id);
            (string cls, string text) = Badge(site, d, today);
            string good = Good("Kids", d.Kids) + Good("Dogs", d.Dogs) + Good("Cats", d.Cats);
            return "<article class=\"dd-dc\" data-did=\"" + Ui.Attr(d.Id) + "\"><a class=\"dd-dc-img\" href=\"" + href + "\" tabindex=\"-1\" aria-hidden=\"true\">" + Ui.Pic(Ui.Cover(site, d), string.Empty, string.Empty, true) + (text != string.Empty ? "<span class=\"dd-badge dd-b-" + cls + "\">" + text + "</span>" : string.Empty) + (d.Photos.Any(Ui.IsVideo) ? "<span class=\"dd-vb\">Video</span>" : string.Empty) + "</a><button type=\"button\" class=\"dd-heart\" data-heart=\"" + Ui.Attr(d.Id) + "\" aria-pressed=\"false\" aria-label=\"Save this dog\" onclick=\"DogJs.heart(this)\"><svg viewBox=\"0 0 24 24\" width=\"20\" height=\"20\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M12 20s-7.5-4.6-7.5-10.2A4.3 4.3 0 0 1 12 7.3a4.3 4.3 0 0 1 7.5 2.5C19.5 15.4 12 20 12 20z\"/></svg></button><div class=\"dd-dc-b\"><span class=\"dd-dname\"><a href=\"" + href + "\">" + Ui.Enc(d.Name) + "</a></span><span class=\"dd-dbreed\">" + Ui.Enc(d.Breed) + "</span><span class=\"dd-dc-f\">" + Facts(d, today) + "</span>" + (good != string.Empty ? "<span class=\"dd-dc-m\"><span>Good with</span>" + good + "</span>" : string.Empty) + "</div></article>";
        }

        private static string Yn(string v)
        {
            return v == "yes" ? "<span class=\"dd-yn dd-yes\"><svg viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.4\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M5 12.5l4.5 4.5L19 7.5\"/></svg> Yes</span>" : v == "no" ? "<span class=\"dd-yn dd-noo\"><svg viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.4\" stroke-linecap=\"round\" aria-hidden=\"true\"><path d=\"M7 7l10 10M17 7L7 17\"/></svg> No</span>" : "<span class=\"dd-yn\">Ask us</span>";
        }

        private static string Tick(bool on, string text)
        {
            return "<li class=\"" + (on ? "dd-on" : "dd-off") + "\">" + (on ? "<svg viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.4\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M5 12.5l4.5 4.5L19 7.5\"/></svg>" : "<svg viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.4\" stroke-linecap=\"round\" aria-hidden=\"true\"><path d=\"M7 7l10 10M17 7L7 17\"/></svg>") + "<span>" + text + "</span></li>";
        }

        private async Task<View> Render(SiteData site)
        {
            DateTime today = Store.Today(site);
            string id = Q("id", 20);
            Pet? d = site.Dogs.FirstOrDefault(x => x.Id == id);
            if (d == null || !(Ui.Public(d) || d.Status == "adopted"))
            {
                return new View { Title = "Dog not found", Main = "<section class=\"dd-phead dd-wrap\"><h1>We could not find that dog</h1><p class=\"dd-lead\">The link may be old, or the dog may not be on the website yet.</p><p style=\"margin-top:20px\"><a class=\"dd-btn dd-btn-accent\" href=\"Dogs\">See all dogs</a></p></section>", Nav = "dogs" };
            }
            if (d.Status == "adopted")
            {
                return new View { Title = d.Name + " is home", Main = "<section class=\"dd-wrap dd-home\">" + Ui.Pic(Ui.Cover(site, d), d.Name, string.Empty, false, true) + "<div><span class=\"dd-kick\">Happy tail</span><h1>" + Ui.Enc(d.Name) + " found a home</h1><p class=\"dd-lead\">" + Ui.Enc(d.HappyTail != string.Empty ? d.HappyTail : d.Name + " was adopted and is settling in with a new family.") + "</p><div class=\"dd-acts\"><a class=\"dd-btn dd-btn-accent\" href=\"Dogs\">Meet the dogs still waiting</a><a class=\"dd-btn dd-btn-ghost\" href=\"Stories\">More happy tails</a></div></div></section>", Nav = "stories" };
            }
            using (await Store.Hold())
            {
                SiteData live = await Store.LoadSite();
                Pet? lv = live.Dogs.FirstOrDefault(x => x.Id == d.Id);
                if (lv != null)
                {
                    lv.Views++;
                    await Store.SaveSite(live);
                }
            }
            string first = Ui.Enc(d.Name);
            string href = "Dog?id=" + Uri.EscapeDataString(d.Id);
            Pet? pair = d.Bonded != string.Empty ? Ui.DogOf(site, d.Bonded) : null;
            var pics = d.Photos.Select((p, i) => new { f = p, t = Ui.Poster(site, p), v = Ui.IsVideo(p), c = d.Name + (Ui.IsVideo(p) ? ", video" : ", photo " + (i + 1)) });
            StringBuilder sb = new StringBuilder("<div class=\"dd-wrap dd-crumb\" style=\"padding-top:22px\"><a href=\"Dogs\">Adopt a dog</a><span>/</span><span>" + first + "</span></div>");
            sb.Append("<div class=\"dd-wrap dd-dtop\"><div class=\"dd-gal\" id=\"dd-gal\" data-photos=\"" + Ui.Attr(JsonSerializer.Serialize(pics)) + "\">");
            for (int i = 0; i < d.Photos.Count && i < 3; i++)
            {
                bool vid = Ui.IsVideo(d.Photos[i]);
                sb.Append("<button type=\"button\" class=\"dd-gi dd-gi" + i + "\" onclick=\"DogJs.show(" + i + ")\" aria-label=\"Open " + (vid ? "video" : "photo") + " " + (i + 1) + " of " + d.Photos.Count + "\">" + Ui.Pic(vid ? Ui.Poster(site, d.Photos[i]) : i == 0 ? d.Photos[i] : Ui.Thumb(d.Photos[i]), d.Name, string.Empty, false, i == 0) + (vid ? "<span class=\"dd-gplay\"></span>" : string.Empty) + "</button>");
            }
            sb.Append("</div><aside class=\"dd-card\"><div class=\"dd-cardh\"><div><h1>" + first + "</h1><p>" + Ui.Enc(d.Breed) + "</p></div><button type=\"button\" class=\"dd-ibtn dd-hbtn\" data-heart=\"" + Ui.Attr(d.Id) + "\" aria-pressed=\"false\" aria-label=\"Save this dog\" onclick=\"DogJs.heart(this)\"><svg viewBox=\"0 0 24 24\" width=\"20\" height=\"20\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M12 20s-7.5-4.6-7.5-10.2A4.3 4.3 0 0 1 12 7.3a4.3 4.3 0 0 1 7.5 2.5C19.5 15.4 12 20 12 20z\"/></svg></button></div>");
            if (d.Status == "hold")
            {
                sb.Append("<p class=\"dd-note dd-n-hold\"><b>Adoption pending.</b> A family is meeting " + first + ". You can still apply in case it does not work out.</p>");
            }
            else if (d.Status == "foster")
            {
                sb.Append("<p class=\"dd-note dd-n-foster\"><b>Living in a foster home.</b> Meetings are arranged by our adoption team after you apply.</p>");
            }
            if (d.Urgent)
            {
                sb.Append("<p class=\"dd-note dd-n-urgent\"><b>Needs a home soon.</b> " + first + " has been with us " + Ui.Plural(Math.Max(1, (today - d.Intake.Date).Days), "day", "days") + ".</p>");
            }
            sb.Append("<dl class=\"dd-facts\"><div><dt>Sex</dt><dd>" + Ui.SexName(d.Sex) + "</dd></div><div><dt>Age</dt><dd>" + Ui.Age(d, today) + "</dd></div><div><dt>Size</dt><dd>" + Ui.SizeName(d.Size) + "</dd></div><div><dt>Weight</dt><dd>" + d.Weight + " lb</dd></div><div><dt>Color</dt><dd>" + Ui.Enc(d.Color) + "</dd></div><div><dt>Energy</dt><dd>" + Ui.NameOf(Ui.Energies, d.Energy) + "</dd></div></dl>");
            string fee = pair != null && d.Fee == 0 ? "Included with " + Ui.Enc(pair.Name) : d.Fee > 0 ? Ui.Money(d.Fee) : "Ask us";
            sb.Append("<div class=\"dd-fee\"><span>Adoption fee</span><b>" + fee + "</b></div><div class=\"dd-cta2\"><a class=\"dd-btn dd-btn-accent dd-btn-wide\" href=\"Adopt?dog=" + Uri.EscapeDataString(d.Id) + "#apply\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"currentColor\" aria-hidden=\"true\"><ellipse cx=\"12\" cy=\"15.5\" rx=\"4.6\" ry=\"3.8\"/><ellipse cx=\"6.4\" cy=\"11\" rx=\"1.8\" ry=\"2.3\"/><ellipse cx=\"9.7\" cy=\"7.3\" rx=\"1.8\" ry=\"2.4\"/><ellipse cx=\"14.3\" cy=\"7.3\" rx=\"1.8\" ry=\"2.4\"/><ellipse cx=\"17.6\" cy=\"11\" rx=\"1.8\" ry=\"2.3\"/></svg> Apply to adopt " + first + "</a><a class=\"dd-btn dd-btn-ghost\" href=\"#dd-ask\">Ask about " + first + "</a><a class=\"dd-btn dd-btn-ghost\" href=\"Donate?dog=" + Uri.EscapeDataString(d.Id) + "\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\" aria-hidden=\"true\"><rect x=\"3.5\" y=\"8\" width=\"17\" height=\"4\" rx=\"1\"/><path d=\"M5 12v8h14v-8M12 8v12M12 8c-1.5-3.5-5.5-4-5.5-1.5S10 8 12 8zM12 8c1.5-3.5 5.5-4 5.5-1.5S14 8 12 8z\"/></svg> Sponsor</a></div><button type=\"button\" class=\"dd-share\" onclick=\"DogJs.share()\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M12 15V4M7.5 8.5L12 4l4.5 4.5\"/><path d=\"M5 13v6h14v-6\"/></svg> Share " + first + "</button></aside></div>");
            sb.Append("<div class=\"dd-wrap dd-dbody\"><div class=\"dd-dmain\"><section><h2>Meet " + first + "</h2>");
            foreach (string para in d.Story.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
            {
                sb.Append("<p class=\"dd-story\">" + Ui.Enc(para) + "</p>");
            }
            if (d.Traits.Count > 0)
            {
                sb.Append("<ul class=\"dd-traits\">");
                foreach (string t in d.Traits)
                {
                    sb.Append("<li><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"currentColor\" aria-hidden=\"true\"><ellipse cx=\"12\" cy=\"15.5\" rx=\"4.6\" ry=\"3.8\"/><ellipse cx=\"6.4\" cy=\"11\" rx=\"1.8\" ry=\"2.3\"/><ellipse cx=\"9.7\" cy=\"7.3\" rx=\"1.8\" ry=\"2.4\"/><ellipse cx=\"14.3\" cy=\"7.3\" rx=\"1.8\" ry=\"2.4\"/><ellipse cx=\"17.6\" cy=\"11\" rx=\"1.8\" ry=\"2.3\"/></svg> " + Ui.Enc(t) + "</li>");
                }
                sb.Append("</ul>");
            }
            sb.Append("</section>");
            if (pair != null && Ui.Public(pair))
            {
                sb.Append("<section class=\"dd-pair\">" + Ui.Pic(Ui.Cover(site, pair), pair.Name, string.Empty, true) + "<div><span class=\"dd-kick\">Bonded pair</span><h2>" + first + " goes home with " + Ui.Enc(pair.Name) + "</h2><p>They have lived together all their lives and must be adopted together, for one adoption fee.</p><a class=\"dd-more\" href=\"Dog?id=" + Uri.EscapeDataString(pair.Id) + "\">Meet " + Ui.Enc(pair.Name) + " &rarr;</a></div></section>");
            }
            sb.Append("<section><h2>Good with</h2><div class=\"dd-good\"><div><span>Children</span>" + Yn(d.Kids) + "</div><div><span>Other dogs</span>" + Yn(d.Dogs) + "</div><div><span>Cats</span>" + Yn(d.Cats) + "</div></div></section>");
            sb.Append("<section><h2>Health and care</h2><ul class=\"dd-ticks\">" + Tick(d.Vaccinated, "Vaccinated") + Tick(d.Fixed, d.Sex == "male" ? "Neutered" : "Spayed") + Tick(d.Chipped, "Microchipped") + Tick(d.HouseTrained, "House-trained") + "</ul>" + (d.Needs != string.Empty ? "<p class=\"dd-note\"><b>Care notes.</b> " + Ui.Enc(d.Needs) + "</p>" : string.Empty) + "<p class=\"dd-hint\">" + Ui.Enc(site.Settings.FeeNote) + "</p></section></div>");
            sb.Append("<aside class=\"dd-askd\" id=\"dd-ask\"><div class=\"dd-box\"><h2>Ask about " + first + "</h2><form class=\"dd-form\" id=\"dd-af\" onsubmit=\"return DogJs.send()\" novalidate><label class=\"dd-field\"><span>Your name</span><input type=\"text\" id=\"dd-aname\" maxlength=\"80\" autocomplete=\"name\"></label><em class=\"dd-err\" id=\"dd-e-aname\" role=\"alert\"></em><label class=\"dd-field\"><span>Email</span><input type=\"email\" id=\"dd-amail\" maxlength=\"120\" autocomplete=\"email\"></label><em class=\"dd-err\" id=\"dd-e-amail\" role=\"alert\"></em><label class=\"dd-field\"><span>Phone <small>(optional)</small></span><input type=\"tel\" id=\"dd-aphone\" maxlength=\"30\" autocomplete=\"tel\"></label><label class=\"dd-field\"><span>Your question</span><textarea id=\"dd-amsg\" rows=\"4\" maxlength=\"1000\" placeholder=\"Is " + first + " okay with stairs? Does " + first + " like car rides?\"></textarea></label><em class=\"dd-err\" id=\"dd-e-amsg\" role=\"alert\"></em><label class=\"dd-hp\" aria-hidden=\"true\">Website<input type=\"text\" id=\"dd-aweb\" tabindex=\"-1\" autocomplete=\"off\"></label><input type=\"hidden\" id=\"dd-adog\" value=\"" + Ui.Attr(d.Id) + "\"><button type=\"submit\" class=\"dd-btn dd-btn-wide\" id=\"dd-asend\">Send question</button><p class=\"dd-hint\">Our adoption team answers within two days.</p></form><div id=\"dd-adone\"></div></div></aside></div>");
            List<Pet> more = site.Dogs.Where(x => x.Id != d.Id && Ui.Adoptable(x) && (x.Size == d.Size || Ui.AgeGroup(x, today) == Ui.AgeGroup(d, today))).OrderByDescending(x => x.Size == d.Size).ThenByDescending(x => x.Listed).Take(4).ToList();
            if (more.Count > 0)
            {
                sb.Append("<section class=\"dd-sec dd-wrap\"><div class=\"dd-shead\"><div><span class=\"dd-kick\">You might also like</span><h2 class=\"dd-h2\">More dogs like " + first + "</h2></div><a class=\"dd-more\" href=\"Dogs\">See all dogs &rarr;</a></div><div class=\"dd-cards dd-c4\">");
                foreach (Pet x in more)
                {
                    sb.Append(Card(site, x, today));
                }
                sb.Append("</div></section>");
            }
            sb.Append("<div class=\"dd-lb\" id=\"dd-lb\" role=\"dialog\" aria-modal=\"true\" aria-label=\"Photos\" hidden><div class=\"dd-lbtop\"><span id=\"dd-lbn\"></span><span id=\"dd-lbc\"></span><button type=\"button\" class=\"dd-ibtn dd-lbx\" aria-label=\"Close photos\" onclick=\"DogJs.hide()\"><svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" aria-hidden=\"true\"><path d=\"M6 6l12 12M18 6L6 18\"/></svg></button></div><div class=\"dd-lbstage\"><button type=\"button\" class=\"dd-lbnav\" aria-label=\"Previous photo\" onclick=\"DogJs.step(-1)\"><svg viewBox=\"0 0 24 24\" width=\"26\" height=\"26\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M15 5l-7 7 7 7\"/></svg></button><img id=\"dd-lbimg\" alt=\"\"><video id=\"dd-lbvid\" controls playsinline hidden></video><button type=\"button\" class=\"dd-lbnav\" aria-label=\"Next photo\" onclick=\"DogJs.step(1)\"><svg viewBox=\"0 0 24 24\" width=\"26\" height=\"26\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M9 5l7 7-7 7\"/></svg></button></div><div class=\"dd-lbthumbs\" id=\"dd-lbthumbs\"></div></div>");
            return new View { Title = d.Name + ", " + d.Breed, Desc = d.Name + " is a " + Ui.Age(d, today) + " old " + d.Breed.ToLowerInvariant() + " looking for a home at " + site.Settings.Name + ".", Main = sb.ToString(), Nav = "dogs" };
        }

        public async Task<ApiResponse> Send()
        {
            ApiResponse response = new ApiResponse();
            string name = Regex.Replace(Ui.Clip(GetDataValue("aname"), 80), "\\s+", " ").Trim();
            string mail = Ui.Clip(GetDataValue("amail"), 120).Trim();
            string msg = Ui.Clip(GetDataValue("amsg"), 1000).Trim();
            if (name.Length < 2)
            {
                response.ExecuteScript("DogJs.fieldErr('aname','Please enter your name.');DogJs.sent(false);");
                return response;
            }
            if (!Ui.Email(mail))
            {
                response.ExecuteScript("DogJs.fieldErr('amail','Enter an email address like name@example.com.');DogJs.sent(false);");
                return response;
            }
            if (msg.Length < 3)
            {
                response.ExecuteScript("DogJs.fieldErr('amsg','Please write your question.');DogJs.sent(false);");
                return response;
            }
            string dogName = string.Empty;
            if (Ui.Clip(GetDataValue("aweb"), 50) == string.Empty)
            {
                using (await Store.Hold())
                {
                    SiteData site = await Store.LoadSite();
                    Pet? d = site.Dogs.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("adog"), 20) && Ui.Public(x));
                    dogName = d?.Name ?? string.Empty;
                    if (!site.Messages.Any(x => x.Email.Equals(mail, StringComparison.OrdinalIgnoreCase) && x.Text == msg))
                    {
                        site.Messages.Add(new Message { Id = Store.NextId(site.Messages.Select(x => x.Id), "M-", 800), Kind = "question", Dog = d?.Id ?? string.Empty, Name = name, Email = mail, Phone = Ui.CleanPhone(GetDataValue("aphone")), Text = msg, Created = DateTime.UtcNow });
                        await Store.SaveSite(site);
                    }
                }
                await Store.Log("web", "question", name + (dogName != string.Empty ? " · " + dogName : string.Empty), true);
            }
            response.ExecuteScript("DogJs.sent(true,'" + Ui.Js("Thanks, " + name.Split(' ')[0] + ". Our adoption team will answer within two days.") + "');");
            return response;
        }
    }
}
