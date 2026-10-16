using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DogShelter.Core;
using DogShelter.Models;
using SkyNet;

namespace DogShelter.codes
{
    public class Admin : WebPage
    {
        private const string AreaKey = "";

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle(site.Settings.Name + " admin | Dashboard");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText.Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--db-accent:" + site.Settings.Accent : string.Empty).Replace("{plhd_name}", Ui.Enc(site.Settings.Name));
        }

        private static void Chrome(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("db-menu", Auth.Menu("db", "Admin", c, site));
            response.SetElementContents("db-who", Auth.Who("db", c));
        }

        private static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript("AdminJs.toast('" + Ui.Js(message) + "');");
            return response;
        }

        private static string NoAccess(string title)
        {
            return "<div class=\"db-ptop\"><div><h1>" + Ui.Enc(title) + "</h1></div></div><div class=\"db-panel\"><p>You don't have access to this area. Ask the shelter manager to give you access in Users.</p></div>";
        }

        public async Task<ApiResponse> SignOut()
        {
            ApiResponse response = new ApiResponse();
            await Auth.Close(GetDataValue("tok"));
            response.ExecuteScript("AdminJs.out();");
            return response;
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Chrome(response, c, site);
            if (AreaKey != string.Empty && !Access.Can(c.U, AreaKey, 'V'))
            {
                await Store.Log(c.U.Id, "view", AreaKey, false);
                response.SetElementContents("db-main", NoAccess("Dashboard"));
                return response;
            }
            response.SetElementContents("db-main", await MainHtml(c, site, GetDataValue("arg")));
            response.ExecuteScript("AdminJs.loaded();");
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
            StringBuilder sb = new StringBuilder("<div class=\"db-tr db-th\">");
            foreach ((string n, string cls) in cols)
            {
                sb.Append("<span" + (cls != string.Empty ? " class=\"" + cls + "\"" : string.Empty) + ">" + n + "</span>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private static string Kpi(string label, string value, string sub, string href, bool hot = false)
        {
            string inner = "<span>" + label + "</span><b>" + value + "</b><small>" + sub + "</small>";
            return href != string.Empty ? "<a class=\"db-kpi" + (hot ? " db-hot" : string.Empty) + "\" href=\"" + href + "\">" + inner + "</a>" : "<div class=\"db-kpi\">" + inner + "</div>";
        }

        private static async Task<string> MainHtml(Ctx c, SiteData site, string arg)
        {
            DateTime now = Store.Now(site);
            DateTime today = now.Date;
            bool canD = Access.Can(c.U, "dogs", 'V');
            bool canA = Access.Can(c.U, "applications", 'V');
            bool canF = Access.Can(c.U, "fosters", 'V');
            bool canV = Access.Can(c.U, "volunteers", 'V');
            bool canE = Access.Can(c.U, "events", 'V');
            bool canM = Access.Can(c.U, "donations", 'V');
            bool canMsg = Access.Can(c.U, "messages", 'V');
            string hello = now.Hour < 12 ? "Good morning" : now.Hour < 18 ? "Good afternoon" : "Good evening";
            StringBuilder sb = new StringBuilder("<div class=\"db-ptop\"><div><h1>" + hello + ", " + Ui.Enc(c.U.Name.Split(' ')[0]) + "</h1><p>" + Ui.LongDay(today) + "</p></div><div class=\"db-acts\">" + (Access.Can(c.U, "dogs", 'E') ? "<a class=\"db-btn db-btn-accent\" href=\"AdminDogs?id=new\">Add a dog</a>" : string.Empty) + "<a class=\"db-btn db-btn-ghost\" href=\"Home\" target=\"_blank\" rel=\"noopener\">View site</a></div></div><div class=\"db-kpis\">");
            if (canD)
            {
                sb.Append(Kpi("Available", site.Dogs.Count(d => d.Status == "available").ToString(Ui.Inv), site.Dogs.Count(d => d.Status == "foster") + " in foster &middot; " + site.Dogs.Count(d => d.Status == "hold") + " pending", "AdminDogs"));
                sb.Append(Kpi("In our care", site.Dogs.Count(d => d.Status != "adopted").ToString(Ui.Inv), site.Dogs.Count(d => d.Status is "intake" or "medical") + " intake or medical", "AdminDogs"));
            }
            if (canA)
            {
                int fresh = site.Applications.Count(x => x.Status == "new");
                sb.Append(Kpi("New applications", fresh.ToString(Ui.Inv), site.Applications.Count(x => x.Status is "review" or "visit" or "approved") + " in progress", "AdminApplications", fresh > 0));
            }
            if (canM)
            {
                DateTime m0 = new DateTime(today.Year, today.Month, 1);
                decimal got = site.Donations.Where(x => x.Status == "received" && x.Received != null && x.Received.Value.Date >= m0).Sum(x => x.Amount);
                decimal open = site.Donations.Where(x => x.Status == "pledged").Sum(x => x.Amount);
                sb.Append(Kpi("Received this month", Ui.Whole(got), Ui.Whole(open) + " pledged, not in yet", "AdminDonations"));
            }
            if (canV)
            {
                sb.Append(Kpi("Volunteers", site.Volunteers.Count(v => v.Status == "active").ToString(Ui.Inv), site.Volunteers.Count(v => v.Status == "new") + " new sign-ups", "AdminVolunteers", site.Volunteers.Any(v => v.Status == "new")));
            }
            if (canE)
            {
                Event? ne = site.Events.Where(e => e.Date.Date >= today).OrderBy(e => e.Date).FirstOrDefault();
                sb.Append(Kpi("Next event", ne != null ? ne.Date.ToString("MMM d", Ui.Inv) : "&ndash;", ne != null ? Ui.Enc(ne.Title) + " &middot; " + ne.Rsvps.Sum(r => r.Guests) + " coming" : "Nothing planned", "AdminEvents"));
            }
            else if (canF)
            {
                sb.Append(Kpi("In foster", site.Placements.Count(p => p.Ended == null).ToString(Ui.Inv), site.Fosters.Count(f => f.Status == "approved") + " approved homes", "AdminFosters"));
            }
            sb.Append("</div><div class=\"db-grid2\" style=\"margin-top:0\">");
            if (canA)
            {
                sb.Append("<section class=\"db-panel\"><div class=\"db-ph\"><h2>Newest applications</h2><a href=\"AdminApplications\">All applications</a></div>");
                List<Application> apps = site.Applications.OrderByDescending(x => x.Created).Take(6).ToList();
                if (apps.Count == 0)
                {
                    sb.Append("<p class=\"db-none\">No applications yet. They arrive here from the Adopt page.</p>");
                }
                foreach (Application x in apps)
                {
                    Pet? d = Ui.DogOf(site, x.Dog);
                    sb.Append("<a class=\"db-row\" href=\"AdminApplications?id=" + Uri.EscapeDataString(x.Id) + "\">" + (d != null ? Ui.Pic(Ui.Thumb(Ui.Cover(site, d)), string.Empty, "db-dth") : string.Empty) + "<div><b>" + Ui.Enc(x.Name) + "</b><small>" + (d != null ? "For " + Ui.Enc(d.Name) : "Any dog") + " &middot; " + Ui.Time(x.Created) + "</small></div>" + Ui.Pill("db", Ui.AppStatuses, x.Status) + "</a>");
                }
                sb.Append("</section>");
            }
            if (canE)
            {
                sb.Append("<section class=\"db-panel\"><div class=\"db-ph\"><h2>Coming up</h2><a href=\"AdminEvents\">All events</a></div>");
                List<Event> evs = site.Events.Where(e => e.Date.Date >= today).OrderBy(e => e.Date).Take(4).ToList();
                if (evs.Count == 0)
                {
                    sb.Append("<p class=\"db-none\">No events planned.</p>");
                }
                foreach (Event e in evs)
                {
                    int g = e.Rsvps.Sum(r => r.Guests);
                    sb.Append("<a class=\"db-row\" href=\"AdminEvents?id=" + Uri.EscapeDataString(e.Id) + "\"><span class=\"db-evd\"><b>" + e.Date.ToString("MMM", Ui.Inv).ToUpperInvariant() + "</b><i>" + e.Date.Day + "</i></span><div><b>" + Ui.Enc(e.Title) + "</b><small>" + Ui.HmText(site, e.Start) + " &middot; " + Ui.Enc(e.Place) + "</small></div><span class=\"db-amt\">" + g + (e.Capacity > 0 ? " / " + e.Capacity : string.Empty) + "</span></a>");
                }
                sb.Append("</section>");
            }
            List<(string Text, int N, string Href)> todo = new List<(string, int, string)>();
            if (canD)
            {
                todo.Add(("Dogs in intake or medical hold over 14 days", site.Dogs.Count(d => d.Status is "intake" or "medical" && d.Intake.Date < today.AddDays(-14)), "AdminDogs?status=medical"));
                todo.Add(("Available dogs with fewer than 3 photos", site.Dogs.Count(d => Ui.Public(d) && d.Photos.Count < 3), "AdminDogs"));
                todo.Add(("Dogs waiting more than 90 days", site.Dogs.Count(d => Ui.Adoptable(d) && d.Intake.Date < today.AddDays(-90)), "AdminDogs?sort=longest"));
            }
            if (canF)
            {
                todo.Add(("Foster stays ending in the next 7 days", site.Placements.Count(p => p.Ended == null && p.Until != null && p.Until.Value.Date <= today.AddDays(7)), "AdminFosters?view=stays"));
                todo.Add(("New foster sign-ups", site.Fosters.Count(f => f.Status == "new"), "AdminFosters?status=new"));
            }
            if (canV)
            {
                todo.Add(("New volunteer sign-ups", site.Volunteers.Count(v => v.Status == "new"), "AdminVolunteers?status=new"));
            }
            if (canM)
            {
                todo.Add(("Pledges older than 30 days, not received", site.Donations.Count(x => x.Status == "pledged" && x.Created < DateTime.UtcNow.AddDays(-30)), "AdminDonations?status=pledged"));
            }
            if (canMsg)
            {
                todo.Add(("New messages", site.Messages.Count(m => m.Status == "new"), "AdminMessages"));
            }
            if (todo.Count > 0)
            {
                sb.Append("<section class=\"db-panel\"><h2>Needs attention</h2><div class=\"db-todo\">");
                int shown = 0;
                foreach ((string t, int n, string h) in todo.Where(x => x.N > 0))
                {
                    sb.Append("<a href=\"" + h + "\"><span>" + t + "</span><b>" + n + "</b></a>");
                    shown++;
                }
                if (shown == 0)
                {
                    sb.Append("<p class=\"db-none\">All clear. Nothing needs attention right now.</p>");
                }
                sb.Append("</div></section>");
            }
            if (canM)
            {
                sb.Append("<section class=\"db-panel\"><div class=\"db-ph\"><h2>Latest gifts</h2><a href=\"AdminDonations\">All donations</a></div>");
                foreach (Donation x in site.Donations.OrderByDescending(x => x.Created).Take(6))
                {
                    sb.Append("<a class=\"db-row\" href=\"AdminDonations?id=" + Uri.EscapeDataString(x.Id) + "\"><div><b>" + Ui.Enc(x.Name) + "</b><small>" + Ui.Enc(Ui.NameOf(Ui.Funds, x.Fund)) + (x.Every == "monthly" ? " &middot; monthly" : string.Empty) + " &middot; " + Ui.Time(x.Created) + "</small></div><span class=\"db-amt\">" + Ui.Money(x.Amount) + "</span>" + Ui.Pill("db", Ui.DonStatuses, x.Status) + "</a>");
                }
                sb.Append("</section>");
            }
            else if (canD)
            {
                sb.Append("<section class=\"db-panel\"><div class=\"db-ph\"><h2>Most viewed dogs</h2><a href=\"AdminDogs\">All dogs</a></div>");
                foreach (Pet d in site.Dogs.Where(Ui.Public).OrderByDescending(d => d.Views).Take(5))
                {
                    sb.Append("<a class=\"db-row\" href=\"AdminDogs?id=" + Uri.EscapeDataString(d.Id) + "\">" + Ui.Pic(Ui.Thumb(Ui.Cover(site, d)), string.Empty, "db-dth") + "<div><b>" + Ui.Enc(d.Name) + "</b><small>" + Ui.Enc(d.Breed) + "</small></div><span class=\"db-amt\">" + Ui.Num(d.Views) + " views</span></a>");
                }
                sb.Append("</section>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }
    }
}
