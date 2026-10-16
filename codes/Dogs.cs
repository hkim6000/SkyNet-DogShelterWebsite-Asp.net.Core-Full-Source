using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DogShelter.Core;
using DogShelter.Models;
using SkyNet;

namespace DogShelter.codes
{
    public class Dogs : WebPage
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
                .Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--dg-accent:" + site.Settings.Accent : string.Empty)
                .Replace("{plhd_nav}", Nav("dg", v.Nav, false))
                .Replace("{plhd_drawer}", Nav("dg", v.Nav, true))
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
                return "<i class=\"dg-shut\"></i>Visiting hours today from " + Ui.HmText(site, d!.Start);
            }
            for (int i = 1; i <= 7; i++)
            {
                DateTime n = now.Date.AddDays(i);
                OpenDay? o = site.Settings.Hours.FirstOrDefault(h => h.Day == (int)n.DayOfWeek);
                if (o != null && o.Open)
                {
                    return "<i class=\"dg-shut\"></i>Closed now &middot; open " + (i == 1 ? "tomorrow" : Ui.DayNames[(int)n.DayOfWeek]) + " from " + Ui.HmText(site, o.Start);
                }
            }
            return "<i class=\"dg-shut\"></i>Closed";
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
                response.SetElementContents("dg-e-nlmail", "Enter an email address like name@example.com.");
                return response;
            }
            bool added = await Store.AddSubscriber(mail);
            response.ExecuteScript("DogsJs.subscribed('" + (added ? "Thanks. Watch your inbox for new dogs and events." : "You are already on the list.") + "');");
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
            return v == "ask" ? string.Empty : "<span class=\"dg-gw" + (v == "no" ? " dg-no" : string.Empty) + "\">" + what + "</span>";
        }

        private static string Card(SiteData site, Pet d, DateTime today)
        {
            string href = "Dog?id=" + Uri.EscapeDataString(d.Id);
            (string cls, string text) = Badge(site, d, today);
            string good = Good("Kids", d.Kids) + Good("Dogs", d.Dogs) + Good("Cats", d.Cats);
            return "<article class=\"dg-dc\" data-did=\"" + Ui.Attr(d.Id) + "\"><a class=\"dg-dc-img\" href=\"" + href + "\" tabindex=\"-1\" aria-hidden=\"true\">" + Ui.Pic(Ui.Cover(site, d), string.Empty, string.Empty, true) + (text != string.Empty ? "<span class=\"dg-badge dg-b-" + cls + "\">" + text + "</span>" : string.Empty) + (d.Photos.Any(Ui.IsVideo) ? "<span class=\"dg-vb\">Video</span>" : string.Empty) + "</a><button type=\"button\" class=\"dg-heart\" data-heart=\"" + Ui.Attr(d.Id) + "\" aria-pressed=\"false\" aria-label=\"Save this dog\" onclick=\"DogsJs.heart(this)\"><svg viewBox=\"0 0 24 24\" width=\"20\" height=\"20\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M12 20s-7.5-4.6-7.5-10.2A4.3 4.3 0 0 1 12 7.3a4.3 4.3 0 0 1 7.5 2.5C19.5 15.4 12 20 12 20z\"/></svg></button><div class=\"dg-dc-b\"><span class=\"dg-dname\"><a href=\"" + href + "\">" + Ui.Enc(d.Name) + "</a></span><span class=\"dg-dbreed\">" + Ui.Enc(d.Breed) + "</span><span class=\"dg-dc-f\">" + Facts(d, today) + "</span>" + (good != string.Empty ? "<span class=\"dg-dc-m\"><span>Good with</span>" + good + "</span>" : string.Empty) + "</div></article>";
        }

        private const int Per = 12;

        private sealed class Filter
        {
            public string Q { get; set; } = string.Empty;
            public string Size { get; set; } = string.Empty;
            public string Age { get; set; } = string.Empty;
            public string Sex { get; set; } = string.Empty;
            public string Good { get; set; } = string.Empty;
            public string Energy { get; set; } = string.Empty;
            public string Sort { get; set; } = "new";
            public bool Pending { get; set; } = true;
            public bool Saved { get; set; }
            public List<string> Ids { get; set; } = new List<string>();
            public int Page { get; set; } = 1;
        }

        private static readonly (string Key, string Name)[] Sorts = { ("new", "Newest first"), ("longest", "Waiting longest"), ("young", "Youngest first"), ("old", "Oldest first"), ("name", "Name, A to Z") };

        private static Filter Read(Func<string, string> get)
        {
            Filter f = new Filter
            {
                Q = Ui.Clip(get("q"), 40),
                Size = Ui.Sizes.Any(x => x.Key == get("size")) ? get("size") : string.Empty,
                Age = get("age") is "puppy" or "adult" or "senior" ? get("age") : string.Empty,
                Sex = get("sex") is "male" or "female" ? get("sex") : string.Empty,
                Good = get("good") is "kids" or "dogs" or "cats" ? get("good") : string.Empty,
                Energy = Ui.Energies.Any(x => x.Key == get("energy")) ? get("energy") : string.Empty,
                Sort = Sorts.Any(x => x.Key == get("sort")) ? get("sort") : "new",
                Pending = get("pending") != "0",
                Saved = get("saved") == "1",
                Ids = get("ids").Split(',').Select(x => Ui.Clip(x, 20)).Where(x => x != string.Empty).Take(200).ToList(),
                Page = int.TryParse(get("pg"), out int p) ? Math.Max(1, p) : 1
            };
            return f;
        }

        private static List<Pet> Apply(SiteData site, Filter f, DateTime today)
        {
            IEnumerable<Pet> q = site.Dogs.Where(Ui.Public);
            if (f.Saved)
            {
                q = q.Where(d => f.Ids.Contains(d.Id));
            }
            if (!f.Pending)
            {
                q = q.Where(d => d.Status != "hold");
            }
            if (f.Q != string.Empty)
            {
                q = q.Where(d => (d.Name + " " + d.Breed + " " + d.Color + " " + d.Id).Contains(f.Q, StringComparison.OrdinalIgnoreCase));
            }
            if (f.Size != string.Empty)
            {
                q = q.Where(d => d.Size == f.Size);
            }
            if (f.Age != string.Empty)
            {
                q = q.Where(d => Ui.AgeGroup(d, today) == f.Age);
            }
            if (f.Sex != string.Empty)
            {
                q = q.Where(d => d.Sex == f.Sex);
            }
            if (f.Energy != string.Empty)
            {
                q = q.Where(d => d.Energy == f.Energy);
            }
            if (f.Good != string.Empty)
            {
                q = q.Where(d => (f.Good == "kids" ? d.Kids : f.Good == "dogs" ? d.Dogs : d.Cats) == "yes");
            }
            q = f.Sort switch
            {
                "longest" => q.OrderBy(d => d.Intake),
                "young" => q.OrderByDescending(d => d.Born),
                "old" => q.OrderBy(d => d.Born),
                "name" => q.OrderBy(d => d.Name),
                _ => q.OrderByDescending(d => d.Listed ?? d.Intake)
            };
            return q.ToList();
        }

        private static string Results(SiteData site, Filter f, DateTime today, string p)
        {
            List<Pet> all = Apply(site, f, today);
            int pages = Math.Max(1, (all.Count + Per - 1) / Per);
            int page = Math.Min(f.Page, pages);
            StringBuilder sb = new StringBuilder("<div class=\"" + p + "-rhead\"><b>" + (f.Saved ? Ui.Plural(all.Count, "saved dog", "saved dogs") : Ui.Plural(all.Count, "dog", "dogs") + " looking for a home") + "</b></div>");
            if (all.Count == 0)
            {
                sb.Append("<div class=\"" + p + "-empty\"><p>" + (f.Saved ? "You have not saved any dogs yet. Tap the heart on a dog to keep it here." : "No dogs match all of these filters. Try removing one.") + "</p><button type=\"button\" class=\"" + p + "-btn " + p + "-btn-ghost\" onclick=\"DogsJs.clearAll()\">" + (f.Saved ? "See all dogs" : "Clear filters") + "</button></div>");
                return sb.ToString();
            }
            sb.Append("<div class=\"" + p + "-cards\">");
            foreach (Pet d in all.Skip((page - 1) * Per).Take(Per))
            {
                sb.Append(Card(site, d, today));
            }
            sb.Append("</div>");
            if (pages > 1)
            {
                sb.Append("<div class=\"" + p + "-pager\"><button type=\"button\" class=\"" + p + "-btn " + p + "-btn-sm " + p + "-btn-ghost\" onclick=\"DogsJs.page(" + (page - 1) + ")\"" + (page <= 1 ? " disabled" : string.Empty) + ">&larr; Previous</button><span>Page " + page + " of " + pages + "</span><button type=\"button\" class=\"" + p + "-btn " + p + "-btn-sm " + p + "-btn-ghost\" onclick=\"DogsJs.page(" + (page + 1) + ")\"" + (page >= pages ? " disabled" : string.Empty) + ">Next &rarr;</button></div>");
            }
            return sb.ToString();
        }

        private async Task<View> Render(SiteData site)
        {
            DateTime today = Store.Today(site);
            Filter f = Read(k => Q(k, 400));
            List<Pet> pub = site.Dogs.Where(Ui.Public).ToList();
            StringBuilder sb = new StringBuilder("<section class=\"dg-phead dg-wrap\"><span class=\"dg-kick\">Adopt a dog</span><h1>" + (f.Saved ? "Your saved dogs" : "Meet the dogs") + "</h1><p class=\"dg-lead\">" + (f.Saved ? "Dogs you saved on this device. They stay here until you remove them." : "Every dog is vaccinated, microchipped and spayed or neutered before going home. Tap a dog to read their story.") + "</p></section>");
            sb.Append("<div class=\"dg-wrap dg-fbar\" id=\"dg-fbar\"><label class=\"dg-qbox\"><span class=\"dg-sr\">Search</span><svg viewBox=\"0 0 24 24\" width=\"20\" height=\"20\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\" aria-hidden=\"true\"><circle cx=\"11\" cy=\"11\" r=\"6.5\"/><path d=\"M16 16l4.5 4.5\"/></svg><input type=\"search\" id=\"dg-q\" list=\"dg-qlist\" placeholder=\"Name or breed\" maxlength=\"40\" value=\"" + Ui.Attr(f.Q) + "\" oninput=\"DogsJs.typed()\"></label>" + Ui.DataList("dg-qlist", pub.Select(d => d.Name).Concat(pub.Select(d => d.Breed))));
            sb.Append(Sel("size", "Size", "Any size", Ui.Sizes.Select(x => (x.Key, x.Name)), f.Size));
            sb.Append(Sel("age", "Age", "Any age", new[] { ("puppy", "Puppy"), ("adult", "Adult"), ("senior", "Senior") }, f.Age));
            sb.Append(Sel("sex", "Sex", "Male or female", new[] { ("female", "Female"), ("male", "Male") }, f.Sex));
            sb.Append(Sel("good", "Good with", "Good with anyone", new[] { ("kids", "Good with kids"), ("dogs", "Good with dogs"), ("cats", "Good with cats") }, f.Good));
            sb.Append(Sel("energy", "Energy", "Any energy", Ui.Energies.Select(x => (x.Key, x.Name)), f.Energy));
            sb.Append(Sel("sort", "Sort", string.Empty, Sorts.Select(x => (x.Key, x.Name)), f.Sort));
            sb.Append("<label class=\"dg-ck\"><input type=\"checkbox\" id=\"dg-pending\"" + (f.Pending ? " checked" : string.Empty) + " onchange=\"DogsJs.go()\"> Show adoption pending</label><input type=\"hidden\" id=\"dg-saved\" value=\"" + (f.Saved ? "1" : string.Empty) + "\"></div>");
            sb.Append("<section class=\"dg-wrap\" id=\"dg-results\" aria-live=\"polite\">" + (f.Saved ? "<p class=\"dg-none\">Loading your saved dogs&hellip;</p>" : Results(site, f, today, "dg")) + "</section>");
            sb.Append("<section class=\"dg-sec dg-wrap\"><div class=\"dg-help\"><div><h2 class=\"dg-h2\">Not sure which dog is right?</h2><p class=\"dg-lead\">Tell us about your home and routine in the adoption form and leave the dog blank. Our team will suggest dogs who fit.</p></div><div class=\"dg-hero-b\"><a class=\"dg-btn dg-btn-accent\" href=\"Adopt#apply\">Start an application</a><a class=\"dg-btn dg-btn-ghost\" href=\"Foster\">Foster first</a></div></div></section>");
            return new View { Title = f.Saved ? "Saved dogs" : "Adoptable dogs", Desc = "Adoptable dogs at " + site.Settings.Name + ". Filter by size, age and who they are good with.", Main = sb.ToString(), Nav = "dogs" };
        }

        private static string Sel(string id, string label, string any, IEnumerable<(string, string)> opts, string cur)
        {
            return "<label class=\"dg-sel\"><span class=\"dg-sr\">" + label + "</span><select id=\"dg-" + id + "\" onchange=\"DogsJs.go()\">" + (any != string.Empty ? "<option value=\"\">" + any + "</option>" : string.Empty) + Ui.Opts(opts, cur) + "</select></label>";
        }

        public async Task<ApiResponse> Search()
        {
            ApiResponse response = new ApiResponse();
            SiteData site = await Store.LoadSite();
            Filter f = Read(k => GetDataValue(k));
            response.SetElementContents("dg-results", Results(site, f, Store.Today(site), "dg"));
            response.ExecuteScript("DogsJs.shown();");
            return response;
        }
    }
}
