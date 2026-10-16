using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DogShelter.Core;
using DogShelter.Models;
using SkyNet;

namespace DogShelter.codes
{
    public class AdminSettings : WebPage
    {
        private const string AreaKey = "settings";

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle(site.Settings.Name + " admin | Settings");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText.Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--st-accent:" + site.Settings.Accent : string.Empty).Replace("{plhd_name}", Ui.Enc(site.Settings.Name));
        }

        private static void Chrome(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("st-menu", Auth.Menu("st", "AdminSettings", c, site));
            response.SetElementContents("st-who", Auth.Who("st", c));
        }

        private static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript("AdminSettingsJs.toast('" + Ui.Js(message) + "');");
            return response;
        }

        private static string NoAccess(string title)
        {
            return "<div class=\"st-ptop\"><div><h1>" + Ui.Enc(title) + "</h1></div></div><div class=\"st-panel\"><p>You don't have access to this area. Ask the shelter manager to give you access in Users.</p></div>";
        }

        public async Task<ApiResponse> SignOut()
        {
            ApiResponse response = new ApiResponse();
            await Auth.Close(GetDataValue("tok"));
            response.ExecuteScript("AdminSettingsJs.out();");
            return response;
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminSettingsJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Chrome(response, c, site);
            if (AreaKey != string.Empty && !Access.Can(c.U, AreaKey, 'V'))
            {
                await Store.Log(c.U.Id, "view", AreaKey, false);
                response.SetElementContents("st-main", NoAccess("Settings"));
                return response;
            }
            response.SetElementContents("st-main", await MainHtml(c, site, GetDataValue("arg")));
            response.ExecuteScript("AdminSettingsJs.loaded();");
            return response;
        }

        private static string Per(string v)
        {
            return v is "today" or "7" or "30" or "range" ? v : "all";
        }

        private static string Who(Dictionary<string, Staff> staff, string id)
        {
            return id == "web" ? "Website" : staff.TryGetValue(id, out Staff? s) ? s.Name : id;
        }

        private static string Head((string Name, string Cls)[] cols)
        {
            StringBuilder sb = new StringBuilder("<div class=\"st-tr st-th\">");
            foreach ((string n, string cls) in cols)
            {
                sb.Append("<span" + (cls != string.Empty ? " class=\"" + cls + "\"" : string.Empty) + ">" + n + "</span>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private static readonly string[] Zones = { "America/New_York", "America/Chicago", "America/Denver", "America/Phoenix", "America/Los_Angeles", "America/Anchorage", "Pacific/Honolulu", "America/Toronto", "America/Vancouver", "Europe/London", "Europe/Paris", "Europe/Berlin", "Asia/Tokyo", "Asia/Seoul", "Australia/Sydney", "Pacific/Auckland", "UTC" };

        private static string Field(string id, string label, string value, int max, string type = "text", string extra = "")
        {
            return "<label class=\"st-field\"><span>" + label + "</span><input type=\"" + type + "\" id=\"st-s" + id + "\" maxlength=\"" + max + "\" value=\"" + Ui.Attr(value) + "\"" + extra + "></label>";
        }

        private static string TextArea(string id, string label, string value, int max, int rows)
        {
            return "<label class=\"st-field\"><span>" + label + "</span><textarea id=\"st-s" + id + "\" rows=\"" + rows + "\" maxlength=\"" + max + "\">" + Ui.Enc(value) + "</textarea></label>";
        }

        private static async Task<string> MainHtml(Ctx c, SiteData site, string arg)
        {
            Settings s = site.Settings;
            StringBuilder sb = new StringBuilder("<div class=\"st-ptop\"><div><h1>Settings</h1><p>Shelter details, adoption fees, donations and visiting hours. Page text and pictures are in Pages.</p></div></div><nav class=\"st-setnav\"><a href=\"#s-shelter\">Shelter</a><a href=\"#s-fees\">Adoption fees</a><a href=\"#s-give\">Donations</a><a href=\"#s-hours\">Visiting hours</a></nav><form onsubmit=\"return AdminSettingsJs.save()\" novalidate>");
            sb.Append("<section class=\"st-panel\" id=\"s-shelter\"><h2>Shelter</h2><div class=\"st-form\"><div class=\"st-f3\">" + Field("name", "Name", s.Name, 40) + Field("town", "Town", s.Town, 40) + Field("tag", "Tagline", s.Tagline, 140) + "</div><div class=\"st-f3\">" + Field("addr", "Address", s.Address, 100) + Field("phone", "Phone", s.Phone, 30, "tel") + Field("email", "Email", s.Email, 80, "email") + "</div>" + Field("char", "Nonprofit line (footer, donation page)", s.Charity, 160) + "<div class=\"st-f4\"><label class=\"st-field\"><span>Accent colour</span><input type=\"color\" id=\"st-saccent\" value=\"" + Ui.Attr(Ui.SafeColor(s.Accent) ? s.Accent : "#9A5B2E") + "\"></label><label class=\"st-field\"><span>Website theme</span><select id=\"st-stheme\">" + Ui.Opts(new[] { ("light", "Light"), ("dark", "Dark") }, s.Theme) + "</select></label></div></div></section>");
            sb.Append("<section class=\"st-panel\" id=\"s-fees\"><h2>Adoption fees</h2><div class=\"st-form\"><div class=\"st-f3\">" + Field("fpup", "Puppies, under 1 year ($)", s.FeePuppy.ToString("0.##", Ui.Inv), 8, "text", " inputmode=\"decimal\"") + Field("fadult", "Adults, 1 to 7 years ($)", s.FeeAdult.ToString("0.##", Ui.Inv), 8, "text", " inputmode=\"decimal\"") + Field("fsen", "Seniors, 8 years and over ($)", s.FeeSenior.ToString("0.##", Ui.Inv), 8, "text", " inputmode=\"decimal\"") + "</div>" + Field("fnote", "What every adoption includes", s.FeeNote, 200) + "</div><p class=\"st-hint\" style=\"margin-top:8px\">These show on the How to adopt page. Each dog also has its own fee, set in Dogs.</p></section>");
            sb.Append("<section class=\"st-panel\" id=\"s-give\"><h2>Donations</h2><div class=\"st-form\">" + Field("amts", "Suggested amounts (comma separated)", s.Amounts, 60) + "<div class=\"st-f2\">" + TextArea("pledge", "How to send a pledged gift", s.PledgeText, 400, 4) + TextArea("wish", "Wish list (one item per line)", s.WishList, 800, 4) + "</div></div></section>");
            sb.Append("<section class=\"st-panel\" id=\"s-hours\"><h2>Visiting hours and time zone</h2><div id=\"st-hours\">");
            foreach (int d in new[] { 1, 2, 3, 4, 5, 6, 0 })
            {
                OpenDay o = s.Hours.FirstOrDefault(h => h.Day == d) ?? new OpenDay { Day = d };
                sb.Append("<div class=\"st-orow" + (o.Open ? string.Empty : " st-shut") + "\" data-day=\"" + d + "\"><b>" + Ui.DayNames[d] + "</b><label><input type=\"checkbox\"" + (o.Open ? " checked" : string.Empty) + " onchange=\"AdminSettingsJs.dayOn(this)\" aria-label=\"Open on " + Ui.DayNames[d] + "\"></label><input type=\"time\" step=\"900\" value=\"" + Ui.Attr(o.Start) + "\" aria-label=\"Opens\"><input type=\"time\" step=\"900\" value=\"" + Ui.Attr(o.End) + "\" aria-label=\"Closes\"></div>");
            }
            List<(string, string)> zones = Zones.Select(z => (z, z.Replace('_', ' '))).ToList();
            if (!Zones.Contains(s.TimeZone))
            {
                zones.Insert(0, (s.TimeZone, s.TimeZone));
            }
            sb.Append("</div><p class=\"st-hint\" style=\"margin:8px 0 12px\">Visiting hours show on every page and on the Contact page.</p><div class=\"st-f4\"><label class=\"st-field\"><span>Time zone (now " + Ui.Hm(site, Store.Now(site)) + ")</span><select id=\"st-stz\">" + Ui.Opts(zones, s.TimeZone) + "</select></label><label class=\"st-chk\" style=\"align-self:end;min-height:44px\"><input type=\"checkbox\" id=\"st-sh24\"" + (s.Clock24 ? " checked" : string.Empty) + "> 24-hour clock</label></div></section>");
            sb.Append("<div class=\"st-savebar\"><em class=\"st-err\" id=\"st-e-set\" role=\"alert\"></em><button type=\"submit\" class=\"st-btn st-btn-accent\">Save settings</button></div></form>");
            return sb.ToString();
        }

        private static decimal Dec(string v, decimal min, decimal max, decimal cur)
        {
            return decimal.TryParse((v ?? string.Empty).Replace(",", string.Empty).Replace("$", string.Empty).Trim(), NumberStyles.Number, Ui.Inv, out decimal d) ? Math.Clamp(d, min, max) : cur;
        }

        public async Task<ApiResponse> Save()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminSettingsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "settings", "settings", "Only the shelter manager changes settings.");
            }
            string name = Regex.Replace(Ui.Clip(GetDataValue("sname"), 40), "\\s+", " ").Trim();
            string email = Ui.Clip(GetDataValue("semail"), 80).Trim();
            string tz = Ui.Clip(GetDataValue("stz"), 60);
            List<string> amts = GetDataValue("samts").Split(',').Select(x => x.Trim().Replace("$", string.Empty)).Where(x => decimal.TryParse(x, NumberStyles.Number, Ui.Inv, out decimal v) && v >= 5 && v <= 100000).Distinct().Take(6).ToList();
            string err = name.Length < 2 ? "Enter the shelter name." : email != string.Empty && !Ui.Email(email) ? "Check the email address." : !Store.KnownZone(tz) ? "Choose a time zone." : amts.Count == 0 ? "Enter at least one suggested amount between 5 and 100,000." : string.Empty;
            List<OpenDay> hours = new List<OpenDay>();
            foreach (string row in GetDataValue("shours").Split('~'))
            {
                string[] x = row.Split('|');
                if (x.Length != 4 || !int.TryParse(x[0], out int d) || d < 0 || d > 6 || Store.Hm(x[2]) == null || Store.Hm(x[3]) == null)
                {
                    continue;
                }
                OpenDay o = new OpenDay { Day = d, Open = x[1] == "1", Start = x[2], End = x[3] };
                if (o.Open && string.CompareOrdinal(o.Start, o.End) >= 0 && err == string.Empty)
                {
                    err = Ui.DayNames[d] + ": closing time must be after opening time.";
                }
                hours.Add(o);
            }
            if (err != string.Empty)
            {
                response.SetElementContents("st-e-set", err);
                return response;
            }
            string accent = Ui.SafeColor(GetDataValue("saccent")) ? GetDataValue("saccent") : "#9A5B2E";
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Settings s = site.Settings;
                s.Name = name;
                s.Town = Ui.Clip(GetDataValue("stown"), 40).Trim();
                s.Tagline = Ui.Clip(GetDataValue("stag"), 140).Trim();
                s.Address = Ui.Clip(GetDataValue("saddr"), 100).Trim();
                s.Phone = Ui.CleanPhone(GetDataValue("sphone"));
                s.Email = email;
                s.Charity = Ui.Clip(GetDataValue("schar"), 160).Trim();
                s.Accent = accent;
                s.Theme = GetDataValue("stheme") == "dark" ? "dark" : "light";
                s.FeePuppy = Dec(GetDataValue("sfpup"), 0, 10000, s.FeePuppy);
                s.FeeAdult = Dec(GetDataValue("sfadult"), 0, 10000, s.FeeAdult);
                s.FeeSenior = Dec(GetDataValue("sfsen"), 0, 10000, s.FeeSenior);
                s.FeeNote = Ui.Clip(GetDataValue("sfnote"), 200).Trim();
                s.Amounts = string.Join(",", amts);
                s.PledgeText = Ui.Clip(GetDataValue("spledge"), 400).Trim();
                s.WishList = string.Join("\n", Ui.SplitLines(GetDataValue("swish"), 20, 80));
                s.TimeZone = tz;
                s.Clock24 = GetDataValue("sh24") == "1";
                if (hours.Count == 7)
                {
                    s.Hours = hours.OrderBy(h => h.Day).ToList();
                }
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "settings", name, true);
            response.ExecuteScript("AdminSettingsJs.toast('Settings saved');document.getElementById('st-site').style.setProperty('--st-accent','" + accent + "');");
            return response;
        }
    }
}
