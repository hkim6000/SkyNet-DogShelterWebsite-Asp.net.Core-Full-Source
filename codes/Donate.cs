using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DogShelter.Core;
using DogShelter.Models;
using SkyNet;

namespace DogShelter.codes
{
    public class Donate : WebPage
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
                .Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--dn-accent:" + site.Settings.Accent : string.Empty)
                .Replace("{plhd_nav}", Nav("dn", v.Nav, false))
                .Replace("{plhd_drawer}", Nav("dn", v.Nav, true))
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
                return "<i class=\"dn-shut\"></i>Visiting hours today from " + Ui.HmText(site, d!.Start);
            }
            for (int i = 1; i <= 7; i++)
            {
                DateTime n = now.Date.AddDays(i);
                OpenDay? o = site.Settings.Hours.FirstOrDefault(h => h.Day == (int)n.DayOfWeek);
                if (o != null && o.Open)
                {
                    return "<i class=\"dn-shut\"></i>Closed now &middot; open " + (i == 1 ? "tomorrow" : Ui.DayNames[(int)n.DayOfWeek]) + " from " + Ui.HmText(site, o.Start);
                }
            }
            return "<i class=\"dn-shut\"></i>Closed";
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
                response.SetElementContents("dn-e-nlmail", "Enter an email address like name@example.com.");
                return response;
            }
            bool added = await Store.AddSubscriber(mail);
            response.ExecuteScript("DonateJs.subscribed('" + (added ? "Thanks. Watch your inbox for new dogs and events." : "You are already on the list.") + "');");
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
            return v == "ask" ? string.Empty : "<span class=\"dn-gw" + (v == "no" ? " dn-no" : string.Empty) + "\">" + what + "</span>";
        }

        private static string Card(SiteData site, Pet d, DateTime today)
        {
            string href = "Dog?id=" + Uri.EscapeDataString(d.Id);
            (string cls, string text) = Badge(site, d, today);
            string good = Good("Kids", d.Kids) + Good("Dogs", d.Dogs) + Good("Cats", d.Cats);
            return "<article class=\"dn-dc\" data-did=\"" + Ui.Attr(d.Id) + "\"><a class=\"dn-dc-img\" href=\"" + href + "\" tabindex=\"-1\" aria-hidden=\"true\">" + Ui.Pic(Ui.Cover(site, d), string.Empty, string.Empty, true) + (text != string.Empty ? "<span class=\"dn-badge dn-b-" + cls + "\">" + text + "</span>" : string.Empty) + (d.Photos.Any(Ui.IsVideo) ? "<span class=\"dn-vb\">Video</span>" : string.Empty) + "</a><button type=\"button\" class=\"dn-heart\" data-heart=\"" + Ui.Attr(d.Id) + "\" aria-pressed=\"false\" aria-label=\"Save this dog\" onclick=\"DonateJs.heart(this)\"><svg viewBox=\"0 0 24 24\" width=\"20\" height=\"20\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M12 20s-7.5-4.6-7.5-10.2A4.3 4.3 0 0 1 12 7.3a4.3 4.3 0 0 1 7.5 2.5C19.5 15.4 12 20 12 20z\"/></svg></button><div class=\"dn-dc-b\"><span class=\"dn-dname\"><a href=\"" + href + "\">" + Ui.Enc(d.Name) + "</a></span><span class=\"dn-dbreed\">" + Ui.Enc(d.Breed) + "</span><span class=\"dn-dc-f\">" + Facts(d, today) + "</span>" + (good != string.Empty ? "<span class=\"dn-dc-m\"><span>Good with</span>" + good + "</span>" : string.Empty) + "</div></article>";
        }

        private static List<decimal> Amounts(Settings s)
        {
            List<decimal> list = s.Amounts.Split(',').Select(x => decimal.TryParse(x.Trim(), NumberStyles.Number, Ui.Inv, out decimal v) ? v : 0m).Where(v => v > 0 && v <= 100000).Distinct().Take(6).ToList();
            return list.Count > 0 ? list : new List<decimal> { 25, 50, 100, 250 };
        }

        private async Task<View> Render(SiteData site)
        {
            Pet? sp = Sponsor(site);
            return new View { Title = sp != null ? "Sponsor " + sp.Name : "Donate", Desc = "Donate to " + site.Settings.Name + ". Every gift pays for food, vaccines and surgery.", Main = Page(site, "donate", Store.Today(site), sp != null ? "Sponsor " + sp.Name : string.Empty, sp != null ? "Your gift pays for " + sp.Name + "'s food, vet care and comfy bed until adoption day." : string.Empty), Nav = "donate" };
        }

        private Pet? Sponsor(SiteData site)
        {
            string dogId = Q("dog", 20);
            return site.Dogs.FirstOrDefault(d => d.Id == dogId && Ui.Public(d));
        }

        private string Part(SiteData site, Block b, DateTime today)
        {
            if (b.Source != "donate")
            {
                return string.Empty;
            }
            Settings s = site.Settings;
            List<decimal> amts = Amounts(s);
            string want = Q("amount", 10);
            decimal pre = decimal.TryParse(want, NumberStyles.Number, Ui.Inv, out decimal w) && w > 0 ? w : amts[Math.Min(1, amts.Count - 1)];
            List<Pet> dogs = site.Dogs.Where(Ui.Public).OrderBy(d => d.Name).ToList();
            Pet? sp = Sponsor(site);
            StringBuilder sb = new StringBuilder("<div class=\"dn-wrap dn-dgrid\" id=\"" + Ui.Attr(b.Id) + "\"><div class=\"dn-box\"><form class=\"dn-form\" id=\"dn-form\" onsubmit=\"return DonateJs.send()\" novalidate><fieldset class=\"dn-fset\"><legend class=\"dn-flabel\">How often?</legend><div class=\"dn-seg\"><label class=\"dn-choice\"><input type=\"radio\" name=\"dn-every\" value=\"once\" checked><span>One time</span></label><label class=\"dn-choice\"><input type=\"radio\" name=\"dn-every\" value=\"monthly\"><span>Monthly</span></label></div></fieldset>");
            sb.Append("<fieldset class=\"dn-fset\"><legend class=\"dn-flabel\">Amount</legend><div class=\"dn-choices dn-amts\">");
            bool hit = false;
            foreach (decimal a in amts)
            {
                bool on = a == pre;
                hit |= on;
                sb.Append("<label class=\"dn-choice\"><input type=\"radio\" name=\"dn-amt\" value=\"" + a.ToString("0.##", Ui.Inv) + "\"" + (on ? " checked" : string.Empty) + " onchange=\"DonateJs.amt()\"><span>" + Ui.Money(a) + "</span></label>");
            }
            sb.Append("<label class=\"dn-other\"><span class=\"dn-sr\">Other amount</span><b>$</b><input type=\"text\" id=\"dn-oamt\" inputmode=\"decimal\" maxlength=\"9\" placeholder=\"Other\" value=\"" + (hit ? string.Empty : pre.ToString("0.##", Ui.Inv)) + "\" oninput=\"DonateJs.other()\"></label></div></fieldset><em class=\"dn-err\" id=\"dn-e-oamt\" role=\"alert\"></em>");
            sb.Append("<div class=\"dn-row2\"><label class=\"dn-field\"><span>Where should it go?</span><select id=\"dn-fund\" onchange=\"DonateJs.fund()\">" + Ui.Opts(Ui.Funds.Select(x => (x.Key, x.Name)), sp != null ? "sponsor" : "general") + "</select></label><label class=\"dn-field\" id=\"dn-dogw\"" + (sp != null ? string.Empty : " hidden") + "><span>Which dog?</span><select id=\"dn-dog\"><option value=\"\">Choose a dog</option>" + Ui.Opts(dogs.Select(d => (d.Id, d.Name + " (" + d.Breed + ")")), sp?.Id ?? string.Empty) + "</select></label></div><em class=\"dn-err\" id=\"dn-e-dog\" role=\"alert\"></em>");
            sb.Append("<label class=\"dn-field\"><span>In honor or memory of <small>(optional)</small></span><input type=\"text\" id=\"dn-honor\" maxlength=\"80\" placeholder=\"In memory of Max\"></label>");
            sb.Append("<div class=\"dn-row2\"><label class=\"dn-field\"><span>Your full name</span><input type=\"text\" id=\"dn-name\" maxlength=\"80\" autocomplete=\"name\"></label><label class=\"dn-field\"><span>Email</span><input type=\"email\" id=\"dn-mail\" maxlength=\"120\" autocomplete=\"email\"></label></div><em class=\"dn-err\" id=\"dn-e-name\" role=\"alert\"></em><em class=\"dn-err\" id=\"dn-e-mail\" role=\"alert\"></em><label class=\"dn-field\"><span>Phone <small>(optional)</small></span><input type=\"tel\" id=\"dn-phone\" maxlength=\"30\" autocomplete=\"tel\"></label>");
            sb.Append("<fieldset class=\"dn-fset\"><legend class=\"dn-flabel\">How will you send your gift?</legend><div class=\"dn-choices\">");
            foreach ((string k, string n) in Ui.Methods)
            {
                sb.Append("<label class=\"dn-choice\"><input type=\"radio\" name=\"dn-method\" value=\"" + k + "\"" + (k == "check" ? " checked" : string.Empty) + "><span>" + n + "</span></label>");
            }
            sb.Append("</div></fieldset><label class=\"dn-check\"><input type=\"checkbox\" id=\"dn-anon\"> Keep my name private in our thank-you lists</label><label class=\"dn-field\"><span>Message <small>(optional)</small></span><textarea id=\"dn-msg\" rows=\"2\" maxlength=\"500\"></textarea></label><label class=\"dn-hp\" aria-hidden=\"true\">Website<input type=\"text\" id=\"dn-web\" tabindex=\"-1\" autocomplete=\"off\"></label><button type=\"submit\" class=\"dn-btn dn-btn-accent dn-btn-wide\" id=\"dn-send\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\" aria-hidden=\"true\"><rect x=\"3.5\" y=\"8\" width=\"17\" height=\"4\" rx=\"1\"/><path d=\"M5 12v8h14v-8M12 8v12M12 8c-1.5-3.5-5.5-4-5.5-1.5S10 8 12 8zM12 8c1.5-3.5 5.5-4 5.5-1.5S14 8 12 8z\"/></svg> <span id=\"dn-sumtxt\">Pledge " + Ui.Money(pre) + "</span></button><p class=\"dn-hint\">This is a pledge. No payment is taken on this website. We will send a receipt for your tax records when your gift arrives.</p></form><div id=\"dn-done\"></div></div>");
            sb.Append("<aside class=\"dn-dside\">" + (sp != null ? "<div class=\"dn-box dn-spon\">" + Ui.Pic(Ui.PhotoAt(site, sp, 1), sp.Name, string.Empty, true) + "<b>" + Ui.Enc(sp.Name) + "</b><span>" + Ui.Enc(sp.Breed) + " &middot; " + Ui.Age(sp, today) + "</span><a class=\"dn-more\" href=\"Dog?id=" + Uri.EscapeDataString(sp.Id) + "\">Read " + Ui.Enc(sp.Name) + "'s story &rarr;</a></div>" : b.Media != string.Empty ? "<div class=\"dn-dimg\">" + Mv(site, b.Media, "A happy dog", Ui.IsVideo(b.Media), false) + "</div>" : string.Empty));
            if (b.Items.Count > 0)
            {
                sb.Append("<div class=\"dn-box\">" + (b.Title != string.Empty ? "<h2>" + Ui.Enc(Tok(site, b.Title)) + "</h2>" : string.Empty) + "<ul class=\"dn-impact\">");
                foreach (BlockItem it in b.Items)
                {
                    sb.Append("<li><b>" + Ui.Enc(it.Title) + "</b><span>" + Ui.Enc(it.Text) + "</span></li>");
                }
                sb.Append("</ul></div>");
            }
            sb.Append("<div class=\"dn-box\"><h2>Sending your gift</h2><p>" + Ui.Enc(s.PledgeText) + "</p><p class=\"dn-hint\" style=\"margin-top:10px\">" + Ui.Enc(s.Charity) + "</p></div>");
            List<string> wish = Ui.SplitLines(s.WishList, 20, 80);
            if (wish.Count > 0)
            {
                sb.Append("<div class=\"dn-box\"><h2>Wish list</h2><p class=\"dn-hint\" style=\"margin-bottom:10px\">Drop these off at the front desk during visiting hours.</p><ul class=\"dn-wish\">");
                foreach (string x in wish)
                {
                    sb.Append("<li><svg viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.4\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M5 12.5l4.5 4.5L19 7.5\"/></svg><span>" + Ui.Enc(x) + "</span></li>");
                }
                sb.Append("</ul></div>");
            }
            sb.Append("</aside></div>");
            return sb.ToString();
        }

        public async Task<ApiResponse> Send()
        {
            ApiResponse response = new ApiResponse();
            string raw = Regex.Replace(Ui.Clip(GetDataValue("amount"), 12), "[^0-9.]", string.Empty);
            decimal amount = decimal.TryParse(raw, NumberStyles.Number, Ui.Inv, out decimal a) ? Math.Round(a, 2) : 0m;
            string name = Regex.Replace(Ui.Clip(GetDataValue("name"), 80), "\\s+", " ").Trim();
            string mail = Ui.Clip(GetDataValue("mail"), 120).Trim();
            string fund = Ui.Funds.Any(x => x.Key == GetDataValue("fund")) ? GetDataValue("fund") : "general";
            if (amount < 5 || amount > 100000)
            {
                response.ExecuteScript("DonateJs.fieldErr('oamt','Choose an amount between $5 and $100,000.');DonateJs.sent(false);");
                return response;
            }
            if (name.Length < 2)
            {
                response.ExecuteScript("DonateJs.fieldErr('name','Please enter your name.');DonateJs.sent(false);");
                return response;
            }
            if (!Ui.Email(mail))
            {
                response.ExecuteScript("DonateJs.fieldErr('mail','Enter an email address like name@example.com.');DonateJs.sent(false);");
                return response;
            }
            string id = string.Empty;
            string dogName = string.Empty;
            string every = GetDataValue("every") == "monthly" ? "monthly" : "once";
            string pledge = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                pledge = site.Settings.PledgeText;
                Pet? d = fund == "sponsor" ? site.Dogs.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("dog"), 20) && Ui.Public(x)) : null;
                if (fund == "sponsor" && d == null)
                {
                    response.ExecuteScript("DonateJs.fieldErr('dog','Choose the dog you would like to sponsor.');DonateJs.sent(false);");
                    return response;
                }
                dogName = d?.Name ?? string.Empty;
                if (Ui.Clip(GetDataValue("web"), 50) == string.Empty)
                {
                    Donation? dup = site.Donations.FirstOrDefault(x => x.Email.Equals(mail, StringComparison.OrdinalIgnoreCase) && x.Amount == amount && x.Status == "pledged" && x.Created > DateTime.UtcNow.AddMinutes(-10));
                    if (dup != null)
                    {
                        id = dup.Id;
                    }
                    else
                    {
                        id = Store.NextId(site.Donations.Select(x => x.Id), "DN-", 5000);
                        site.Donations.Insert(0, new Donation { Id = id, Name = name, Email = mail, Phone = Ui.CleanPhone(GetDataValue("phone")), Amount = amount, Every = every, Fund = fund, Dog = d?.Id ?? string.Empty, Honor = Ui.Clip(GetDataValue("honor"), 80), Method = Ui.Methods.Any(x => x.Key == GetDataValue("method")) ? GetDataValue("method") : "check", Anonymous = GetDataValue("anon") == "1", Message = Ui.Clip(GetDataValue("msg"), 500), Created = DateTime.UtcNow });
                        await Store.SaveSite(site);
                    }
                }
            }
            if (id != string.Empty)
            {
                await Store.Log("web", "pledge", id + " · " + name + " · " + amount.ToString("0.##", Ui.Inv), true);
            }
            string what = Ui.Money(amount) + (every == "monthly" ? " a month" : string.Empty) + (dogName != string.Empty ? " for " + dogName : string.Empty);
            response.ExecuteScript("DonateJs.sent(true,'" + Ui.Js(name.Split(' ')[0]) + "','" + Ui.Js(id) + "','" + Ui.Js(System.Net.WebUtility.HtmlDecode(what)) + "','" + Ui.Js(pledge) + "');");
            return response;
        }

        private static string Tok(SiteData site, string v)
        {
            return v.Replace("{name}", site.Settings.Name).Replace("{town}", site.Settings.Town);
        }

        private static string TokH(SiteData site, string v)
        {
            return v.Replace("{name}", Ui.Enc(site.Settings.Name)).Replace("{town}", Ui.Enc(site.Settings.Town));
        }

        private static string Lines(string v)
        {
            return Ui.Enc(v).Replace("\n", "<br>");
        }

        private static string Icon(string k)
        {
            return k switch
            {
                "paw" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"currentColor\" aria-hidden=\"true\"><ellipse cx=\"12\" cy=\"15.5\" rx=\"4.6\" ry=\"3.8\"/><ellipse cx=\"6.4\" cy=\"11\" rx=\"1.8\" ry=\"2.3\"/><ellipse cx=\"9.7\" cy=\"7.3\" rx=\"1.8\" ry=\"2.4\"/><ellipse cx=\"14.3\" cy=\"7.3\" rx=\"1.8\" ry=\"2.4\"/><ellipse cx=\"17.6\" cy=\"11\" rx=\"1.8\" ry=\"2.3\"/></svg>",
                "home" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M4 11l8-7 8 7\"/><path d=\"M6 10v10h12V10\"/></svg>",
                "hand" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M12 20s-7-4.3-7-9.4A3.9 3.9 0 0 1 12 8.4a3.9 3.9 0 0 1 7 2.2C19 15.7 12 20 12 20z\"/></svg>",
                "gift" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\" aria-hidden=\"true\"><rect x=\"3.5\" y=\"8\" width=\"17\" height=\"4\" rx=\"1\"/><path d=\"M5 12v8h14v-8M12 8v12M12 8c-1.5-3.5-5.5-4-5.5-1.5S10 8 12 8zM12 8c1.5-3.5 5.5-4 5.5-1.5S14 8 12 8z\"/></svg>",
                "heart" => "<svg viewBox=\"0 0 24 24\" width=\"20\" height=\"20\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M12 20s-7.5-4.6-7.5-10.2A4.3 4.3 0 0 1 12 7.3a4.3 4.3 0 0 1 7.5 2.5C19.5 15.4 12 20 12 20z\"/></svg>",
                "cal" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\" aria-hidden=\"true\"><rect x=\"3.5\" y=\"5\" width=\"17\" height=\"15\" rx=\"2\"/><path d=\"M3.5 10h17M8 3v4M16 3v4\" stroke-linecap=\"round\"/></svg>",
                "pin" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M12 21s-6.5-6-6.5-11a6.5 6.5 0 0 1 13 0c0 5-6.5 11-6.5 11z\"/><circle cx=\"12\" cy=\"10\" r=\"2.4\"/></svg>",
                "clock" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" aria-hidden=\"true\"><circle cx=\"12\" cy=\"12\" r=\"8.5\"/><path d=\"M12 7.5V12l3 2\"/></svg>",
                "check" => "<svg viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.4\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M5 12.5l4.5 4.5L19 7.5\"/></svg>",
                "phone" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M6.5 3.5h3l1.5 4.5-2 1.5a11 11 0 0 0 5.5 5.5l1.5-2 4.5 1.5v3a2 2 0 0 1-2 2A16 16 0 0 1 4.5 5.5a2 2 0 0 1 2-2z\"/></svg>",
                "mail" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\" aria-hidden=\"true\"><rect x=\"3.5\" y=\"5.5\" width=\"17\" height=\"13\" rx=\"2\"/><path d=\"M4 7l8 6 8-6\"/></svg>",
                "cam" => "<svg viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M4 8h3l1.6-2.5h6.8L17 8h3v11H4z\"/><circle cx=\"12\" cy=\"13.2\" r=\"3.4\"/></svg>",
                _ => string.Empty
            };
        }

        private static string Mv(SiteData site, string src, string alt, bool bg, bool eager)
        {
            if (src == string.Empty)
            {
                return string.Empty;
            }
            MediaItem? m = site.Media.FirstOrDefault(x => x.Full == src);
            int w = m != null && m.Width > 0 ? m.Width : 1600;
            int h = m != null && m.Height > 0 ? m.Height : 1200;
            if (Ui.IsVideo(src))
            {
                return "<video src=\"" + Ui.Attr(src) + "\" poster=\"" + Ui.Attr(m?.Thumb ?? string.Empty) + "\" width=\"" + w + "\" height=\"" + h + "\" playsinline " + (bg ? "autoplay muted loop data-bg=\"1\" aria-hidden=\"true\"" : "controls preload=\"none\" aria-label=\"" + Ui.Attr(alt) + "\"") + "></video>";
            }
            return "<img src=\"" + Ui.Attr(src) + "\" alt=\"" + Ui.Attr(m != null && m.Alt != string.Empty ? m.Alt : alt) + "\" width=\"" + w + "\" height=\"" + h + "\"" + (eager ? string.Empty : " loading=\"lazy\"") + ">";
        }

        private static string Btn(SiteData site, string label, string link, bool main)
        {
            return label == string.Empty || link == string.Empty ? string.Empty : "<a class=\"dn-btn " + (main ? "dn-btn-accent" : "dn-btn-ghost") + "\" href=\"" + Ui.Attr(link) + "\"" + (link.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? " target=\"_blank\" rel=\"noopener\"" : string.Empty) + ">" + Ui.Enc(Tok(site, label)) + "</a>";
        }

        private static string Btns(SiteData site, Block b)
        {
            string x = Btn(site, b.Label, b.Link, true) + Btn(site, b.Label2, b.Link2, false);
            return x == string.Empty ? string.Empty : "<div class=\"dn-hero-b\">" + x + "</div>";
        }

        private static string Kick(SiteData site, Block b)
        {
            return b.Kicker == string.Empty ? string.Empty : "<span class=\"dn-kick\">" + Ui.Enc(Tok(site, b.Kicker)) + "</span>";
        }

        private static string H2(SiteData site, Block b)
        {
            return b.Title == string.Empty ? string.Empty : "<h2 class=\"dn-h2\">" + Ui.Enc(Tok(site, b.Title)) + "</h2>";
        }

        private static string Rich(SiteData site, string html, string cls)
        {
            return html == string.Empty ? string.Empty : "<div class=\"dn-rich " + cls + "\">" + TokH(site, html) + "</div>";
        }

        private static string Head(SiteData site, Block b, bool lead)
        {
            string x = Kick(site, b) + H2(site, b) + (lead ? Rich(site, b.Text, "dn-lead") : string.Empty);
            return x == string.Empty ? string.Empty : "<div class=\"dn-bhead\">" + x + "</div>";
        }

        private static string Sec(Block b, string inner)
        {
            return "<section class=\"dn-sec dn-wrap\" id=\"" + Ui.Attr(b.Id) + "\">" + inner + "</section>";
        }

        private static string Finder()
        {
            return "<form class=\"dn-find\" action=\"Dogs\" method=\"get\"><label class=\"dn-field\"><span>Size</span><select name=\"size\"><option value=\"\">Any size</option>" + Ui.Opts(Ui.Sizes.Select(x => (x.Key, x.Name)), string.Empty) + "</select></label><label class=\"dn-field\"><span>Age</span><select name=\"age\"><option value=\"\">Any age</option>" + Ui.Opts(new[] { ("puppy", "Puppy"), ("adult", "Adult"), ("senior", "Senior") }, string.Empty) + "</select></label><label class=\"dn-field\"><span>Good with</span><select name=\"good\"><option value=\"\">Anyone</option>" + Ui.Opts(new[] { ("kids", "Kids"), ("dogs", "Other dogs"), ("cats", "Cats") }, string.Empty) + "</select></label><button type=\"submit\" class=\"dn-btn\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"currentColor\" aria-hidden=\"true\"><ellipse cx=\"12\" cy=\"15.5\" rx=\"4.6\" ry=\"3.8\"/><ellipse cx=\"6.4\" cy=\"11\" rx=\"1.8\" ry=\"2.3\"/><ellipse cx=\"9.7\" cy=\"7.3\" rx=\"1.8\" ry=\"2.4\"/><ellipse cx=\"14.3\" cy=\"7.3\" rx=\"1.8\" ry=\"2.4\"/><ellipse cx=\"17.6\" cy=\"11\" rx=\"1.8\" ry=\"2.3\"/></svg> Find a dog</button></form>";
        }

        private static string Stats(SiteData site, DateTime today)
        {
            int adopted = site.Dogs.Count(d => d.Adopted != null && d.Adopted.Value.Year == today.Year);
            return "<div class=\"dn-hstats\"><a href=\"Dogs\"><b>" + site.Dogs.Count(Ui.Public) + "</b><span>dogs waiting</span></a><a href=\"Stories\"><b>" + adopted + "</b><span>adopted this year</span></a><a href=\"Foster\"><b>" + site.Placements.Count(x => x.Ended == null) + "</b><span>in foster homes</span></a></div>";
        }

        private static string Hero(SiteData site, Block b, DateTime today, bool h1, string title, string text)
        {
            bool find = b.Extra == "finder";
            string tag = h1 ? "h1" : "h2";
            string media = Mv(site, b.Media, Tok(site, b.Title), Ui.IsVideo(b.Media), h1);
            string t = title != string.Empty ? Ui.Enc(title) : Ui.Enc(Tok(site, b.Title));
            string lead = text != string.Empty ? "<p class=\"dn-lead\">" + Ui.Enc(text) + "</p>" : Rich(site, b.Text, "dn-lead");
            return "<section class=\"dn-bhero" + (find ? " dn-bhome" : string.Empty) + (media == string.Empty ? " dn-bplain" : string.Empty) + "\" id=\"" + Ui.Attr(b.Id) + "\"><div class=\"dn-wrap dn-bhero-in\"><div class=\"dn-bhero-t\">" + Kick(site, b) + "<" + tag + " class=\"dn-btitle\">" + t + "</" + tag + ">" + lead + Btns(site, b) + (find ? Finder() : string.Empty) + "</div>" + (media != string.Empty ? "<div class=\"dn-bhero-p\">" + media + (find ? Stats(site, today) : string.Empty) + "</div>" : string.Empty) + "</div></section>";
        }

        private static string Split(SiteData site, Block b)
        {
            string counts = b.Extra == "counts" ? "<div class=\"dn-nums\"><span><b>" + site.Dogs.Count(d => d.Status != "adopted") + "</b>dogs in our care</span><span><b>" + site.Fosters.Count(f => f.Status == "approved") + "</b>foster homes</span><span><b>" + site.Volunteers.Count(v => v.Status == "active") + "</b>active volunteers</span></div>" : string.Empty;
            string btn = Btn(site, b.Label, b.Link, false);
            return Sec(b, "<div class=\"dn-bsplit" + (b.Flip ? " dn-flip" : string.Empty) + (b.Media == string.Empty ? " dn-nomedia" : string.Empty) + "\">" + (b.Media != string.Empty ? "<div class=\"dn-bsm\">" + Mv(site, b.Media, Tok(site, b.Title), false, false) + "</div>" : string.Empty) + "<div>" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, "dn-lead") + counts + (btn != string.Empty ? "<div class=\"dn-hero-b\">" + btn + "</div>" : string.Empty) + "</div></div>");
        }

        private static string Cards(SiteData site, Block b)
        {
            int n = Math.Clamp(b.Items.Count, 1, 4);
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<div class=\"dn-bcards dn-n" + n + "\">");
            foreach (BlockItem it in b.Items)
            {
                string ico = Icon(it.Icon);
                string inner = (ico != string.Empty ? "<span class=\"dn-wico\">" + ico + "</span>" : string.Empty) + "<b>" + Ui.Enc(Tok(site, it.Title)) + "</b>" + (it.Text != string.Empty ? "<p>" + Lines(Tok(site, it.Text)) + "</p>" : string.Empty);
                if (it.Link != string.Empty)
                {
                    sb.Append("<a class=\"dn-bcard\" href=\"" + Ui.Attr(it.Link) + "\">" + inner + "<span class=\"dn-more\">" + Ui.Enc(it.Label != string.Empty ? it.Label : "Learn more") + " &rarr;</span></a>");
                }
                else
                {
                    sb.Append("<div class=\"dn-bcard\">" + inner + "</div>");
                }
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string Steps(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<ol class=\"dn-bsteps dn-n" + Math.Clamp(b.Items.Count, 1, 5) + "\">");
            int i = 1;
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<li><span>" + i++ + "</span><b>" + Ui.Enc(Tok(site, it.Title)) + "</b><p>" + Lines(Tok(site, it.Text)) + "</p></li>");
            }
            sb.Append("</ol>");
            return Sec(b, sb.ToString());
        }

        private static string Boxes(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, false) + "<div class=\"dn-bboxes dn-n" + Math.Clamp(b.Items.Count, 1, 3) + "\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<div class=\"dn-box\">" + (it.Title != string.Empty ? "<h2>" + Ui.Enc(Tok(site, it.Title)) + "</h2>" : string.Empty) + "<ul class=\"dn-bticks\">");
                foreach (string line in it.Text.Split('\n').Select(x => x.Trim()).Where(x => x != string.Empty))
                {
                    sb.Append("<li><svg viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.4\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M5 12.5l4.5 4.5L19 7.5\"/></svg><span>" + Ui.Enc(Tok(site, line)) + "</span></li>");
                }
                sb.Append("</ul></div>");
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string Faq(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder("<div class=\"dn-bfaq\">" + Head(site, b, false) + "<div class=\"dn-faq\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<details><summary>" + Ui.Enc(Tok(site, it.Title)) + "</summary><p>" + Lines(Tok(site, it.Text)) + "</p></details>");
            }
            sb.Append("</div></div>");
            return Sec(b, sb.ToString());
        }

        private static string Quote(SiteData site, Block b)
        {
            return Sec(b, "<figure class=\"dn-bquote\">" + (b.Media != string.Empty && !Ui.IsVideo(b.Media) ? Mv(site, b.Media, b.Title, false, false) : string.Empty) + "<blockquote>" + Lines(Tok(site, b.Text)) + "</blockquote>" + (b.Title != string.Empty ? "<figcaption>" + Ui.Enc(Tok(site, b.Title)) + "</figcaption>" : string.Empty) + "</figure>");
        }

        private static string Wide(SiteData site, Block b)
        {
            return b.Media == string.Empty ? string.Empty : Sec(b, "<figure class=\"dn-bmedia\">" + Mv(site, b.Media, b.Text, false, false) + (b.Text != string.Empty ? "<figcaption>" + Ui.Enc(Tok(site, b.Text)) + "</figcaption>" : string.Empty) + "</figure>");
        }

        private static string Cta(SiteData site, Block b)
        {
            return Sec(b, "<div class=\"dn-bcta\"><div>" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, "dn-lead") + "</div>" + Btns(site, b) + "</div>");
        }

        private string Page(SiteData site, string key, DateTime today, string title = "", string text = "")
        {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            if (!site.Pages.TryGetValue(key, out PageDoc? doc))
            {
                return string.Empty;
            }
            foreach (Block b in doc.Blocks.Where(x => !x.Hidden))
            {
                string html = b.Type switch
                {
                    "hero" => Hero(site, b, today, first, first ? title : string.Empty, first ? text : string.Empty),
                    "text" => Sec(b, "<div class=\"dn-btext\">" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, string.Empty) + "</div>"),
                    "split" => Split(site, b),
                    "cards" => Cards(site, b),
                    "steps" => Steps(site, b),
                    "boxes" => Boxes(site, b),
                    "faq" => Faq(site, b),
                    "quote" => Quote(site, b),
                    "media" => Wide(site, b),
                    "cta" => Cta(site, b),
                    "data" => Part(site, b, today),
                    _ => string.Empty
                };
                if (b.Type == "hero")
                {
                    first = false;
                }
                sb.Append(html);
            }
            return sb.ToString();
        }
    }
}
