using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DogShelter.Core;
using DogShelter.Models;
using SkyNet;

namespace DogShelter.codes
{
    public class AdminEvents : WebPage
    {
        private const string AreaKey = "events";

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle(site.Settings.Name + " admin | Events");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText.Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--ez-accent:" + site.Settings.Accent : string.Empty).Replace("{plhd_name}", Ui.Enc(site.Settings.Name));
        }

        private static void Chrome(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("ez-menu", Auth.Menu("ez", "AdminEvents", c, site));
            response.SetElementContents("ez-who", Auth.Who("ez", c));
        }

        private static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript("AdminEventsJs.toast('" + Ui.Js(message) + "');");
            return response;
        }

        private static string NoAccess(string title)
        {
            return "<div class=\"ez-ptop\"><div><h1>" + Ui.Enc(title) + "</h1></div></div><div class=\"ez-panel\"><p>You don't have access to this area. Ask the shelter manager to give you access in Users.</p></div>";
        }

        public async Task<ApiResponse> SignOut()
        {
            ApiResponse response = new ApiResponse();
            await Auth.Close(GetDataValue("tok"));
            response.ExecuteScript("AdminEventsJs.out();");
            return response;
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminEventsJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Chrome(response, c, site);
            if (AreaKey != string.Empty && !Access.Can(c.U, AreaKey, 'V'))
            {
                await Store.Log(c.U.Id, "view", AreaKey, false);
                response.SetElementContents("ez-main", NoAccess("Events"));
                return response;
            }
            response.SetElementContents("ez-main", await MainHtml(c, site, GetDataValue("arg")));
            response.ExecuteScript("AdminEventsJs.loaded();");
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
            StringBuilder sb = new StringBuilder("<div class=\"ez-tr ez-th\">");
            foreach ((string n, string cls) in cols)
            {
                sb.Append("<span" + (cls != string.Empty ? " class=\"" + cls + "\"" : string.Empty) + ">" + n + "</span>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private static string PickItem(MediaItem m)
        {
            bool vid = m.Kind == "video";
            return "<button type=\"button\" class=\"ez-pki\" data-src=\"" + Ui.Attr(m.Full) + "\" data-thumb=\"" + Ui.Attr(m.Thumb) + "\" data-kind=\"" + m.Kind + "\" data-title=\"" + Ui.Attr(m.Title) + "\" onclick=\"AdminEventsJs.pkToggle(this)\" title=\"" + Ui.Attr(m.Title + (m.Tags.Count > 0 ? " · " + string.Join(", ", m.Tags) : string.Empty)) + "\"><img src=\"" + Ui.Attr(m.Thumb) + "\" alt=\"\" loading=\"lazy\">" + (vid ? "<b class=\"ez-vbadge\">" + (m.Seconds / 60) + ":" + (m.Seconds % 60).ToString("00", Ui.Inv) + "</b>" : string.Empty) + "<span>" + Ui.Enc(m.Title) + "</span><i>&#10003;</i></button>";
        }

        public async Task<ApiResponse> MediaPick()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminEventsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return response;
            }
            SiteData site = await Store.LoadSite();
            string allow = GetDataValue("pallow") is "photo" or "video" ? GetDataValue("pallow") : string.Empty;
            string kind = allow != string.Empty ? allow : GetDataValue("pkind") is "photo" or "video" ? GetDataValue("pkind") : string.Empty;
            string term = Ui.Clip(GetDataValue("pterm"), 60).Trim();
            string tag = Ui.Clip(GetDataValue("ptag"), 40);
            List<string> ctx = Ui.Clip(GetDataValue("pctx"), 80).ToLowerInvariant().Split(',').Select(x => x.Trim()).Where(x => x != string.Empty).ToList();
            int page = int.TryParse(GetDataValue("ppg"), out int pv) ? Math.Clamp(pv, 1, 200) : 1;
            const int size = 30;
            List<MediaItem> all = site.Media.Where(m => (kind == string.Empty || m.Kind == kind) && (tag == string.Empty || m.Tags.Contains(tag)) && (term == string.Empty || (m.Title + " " + m.Alt + " " + string.Join(" ", m.Tags)).Contains(term, StringComparison.OrdinalIgnoreCase))).OrderByDescending(m => m.Tags.Count(t => ctx.Contains(t))).ThenByDescending(m => m.When).ToList();
            StringBuilder sb = new StringBuilder();
            foreach (MediaItem m in all.Skip((page - 1) * size).Take(size))
            {
                sb.Append(PickItem(m));
            }
            if (all.Count == 0)
            {
                sb.Append("<p class=\"ez-none\" style=\"grid-column:1/-1\">Nothing matches. Upload a photo, or add videos in Media.</p>");
            }
            string tags = page == 1 ? JsonSerializer.Serialize(site.Media.SelectMany(m => m.Tags).Distinct().OrderBy(t => t).ToList()) : "null";
            response.ExecuteScript("AdminEventsJs.pkShown(" + JsonSerializer.Serialize(sb.ToString()) + "," + (all.Count > page * size ? "true" : "false") + "," + tags + "," + (page > 1 ? "true" : "false") + ");");
            return response;
        }

        private static string PickExt(byte[] b)
        {
            if (b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
            {
                return "jpg";
            }
            if (b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47)
            {
                return "png";
            }
            if (b.Length > 12 && b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50)
            {
                return "webp";
            }
            return string.Empty;
        }

        private static byte[]? PickData(string data, int max)
        {
            Match m = Regex.Match(data ?? string.Empty, "^data:image/(jpeg|png|webp);base64,([A-Za-z0-9+/=]+)$");
            if (!m.Success)
            {
                return null;
            }
            try
            {
                byte[] b = Convert.FromBase64String(m.Groups[2].Value);
                return b.Length <= max && PickExt(b) != string.Empty ? b : null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        public async Task<ApiResponse> MediaUp()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminEventsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "upload", "photo", "You can look but not upload.");
            }
            byte[]? big = PickData(GetDataValue("full"), 8 * 1024 * 1024);
            byte[]? small = PickData(GetDataValue("thumb"), 3 * 1024 * 1024);
            if (big == null || small == null || PickExt(big) != PickExt(small))
            {
                response.ExecuteScript("AdminEventsJs.pkUploaded(false,'That file is not a JPG, PNG or WebP photo, or it is too large.');");
                return response;
            }
            string ext = PickExt(big);
            string id = Guid.NewGuid().ToString("N").Substring(0, 16);
            string dir = Path.Combine(Store.Root, "images", "uploads");
            Directory.CreateDirectory(Path.Combine(dir, "full"));
            Directory.CreateDirectory(Path.Combine(dir, "thumbs"));
            await File.WriteAllBytesAsync(Path.Combine(dir, "full", id + "." + ext), big);
            await File.WriteAllBytesAsync(Path.Combine(dir, "thumbs", id + "." + ext), small);
            string title = Regex.Replace(Ui.Clip(GetDataValue("pname"), 80), "\\s+", " ").Trim();
            List<string> ctx = Ui.Clip(GetDataValue("pctx"), 80).ToLowerInvariant().Split(',').Select(x => Regex.Replace(x, "[^a-z0-9 -]", string.Empty).Trim()).Where(x => x != string.Empty).Distinct().Take(4).ToList();
            int w = int.TryParse(GetDataValue("pw"), out int wv) ? Math.Clamp(wv, 0, 20000) : 0;
            int h = int.TryParse(GetDataValue("ph"), out int hv) ? Math.Clamp(hv, 0, 20000) : 0;
            double r = Math.Min(1.0, 1600.0 / Math.Max(1, Math.Max(w, h)));
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                site.Media.Add(new MediaItem { Id = id, Kind = "photo", Title = title.Length >= 2 ? title : "Photo " + id.Substring(0, 6), Tags = ctx, Full = "images/uploads/full/" + id + "." + ext, Thumb = "images/uploads/thumbs/" + id + "." + ext, Width = (int)Math.Round(w * r), Height = (int)Math.Round(h * r), Bytes = big.Length, By = c.U.Id, When = DateTime.UtcNow });
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "upload", "photo · " + title, true);
            response.ExecuteScript("AdminEventsJs.pkUploaded(true,'');");
            return response;
        }

        private const int Size = 20;

        private static string F(string id, string label, string value, int max, string type = "text", string extra = "")
        {
            return "<label class=\"ez-field\"><span>" + label + "</span><input type=\"" + type + "\" id=\"ez-" + id + "\" maxlength=\"" + max + "\" value=\"" + Ui.Attr(value) + "\"" + extra + "></label>";
        }

        private static int Taken(Event e)
        {
            return e.Rsvps.Sum(r => r.Guests);
        }

        private static string Tile(Event e)
        {
            return "<span class=\"ez-evd\"><b>" + e.Date.ToString("MMM", Ui.Inv).ToUpperInvariant() + "</b><i>" + e.Date.Day + "</i><small>" + e.Date.ToString("ddd", Ui.Inv) + "</small></span>";
        }

        private static async Task<string> MainHtml(Ctx c, SiteData site, string arg)
        {
            StringBuilder sb = new StringBuilder("<div class=\"ez-ptop\"><div><h1>Events</h1><p>Open houses, adoption days, classes and fundraisers. Published events show on the website; set places to take reservations.</p></div><div class=\"ez-acts\">" + (Access.Can(c.U, AreaKey, 'E') ? "<button type=\"button\" class=\"ez-btn ez-btn-accent\" onclick=\"AdminEventsJs.edit('')\">Add an event</button>" : string.Empty) + "</div></div>");
            sb.Append("<div class=\"ez-views\"><div class=\"ez-chips\" id=\"ez-vchips\" role=\"group\" aria-label=\"View\"><button type=\"button\" class=\"ez-chip ez-act\" data-v=\"list\" aria-pressed=\"true\" onclick=\"AdminEventsJs.setView('list')\">List</button><button type=\"button\" class=\"ez-chip\" data-v=\"week\" aria-pressed=\"false\" onclick=\"AdminEventsJs.setView('week')\">Week</button><button type=\"button\" class=\"ez-chip\" data-v=\"month\" aria-pressed=\"false\" onclick=\"AdminEventsJs.setView('month')\">Month</button></div><div class=\"ez-calnav\" id=\"ez-calnav\" hidden><button type=\"button\" class=\"ez-ibtn\" aria-label=\"Previous\" onclick=\"AdminEventsJs.cal(-1)\">&lsaquo;</button><b id=\"ez-callabel\"></b><button type=\"button\" class=\"ez-ibtn\" aria-label=\"Next\" onclick=\"AdminEventsJs.cal(1)\">&rsaquo;</button><button type=\"button\" class=\"ez-btn ez-btn-sm ez-btn-ghost\" onclick=\"AdminEventsJs.today()\">Today</button></div></div>");
            sb.Append("<div class=\"ez-bar\"><label id=\"ez-qw\">Search<input type=\"search\" id=\"ez-q\" list=\"ez-qlist\" placeholder=\"Title or place\" maxlength=\"40\" oninput=\"AdminEventsJs.search()\"></label>" + Ui.DataList("ez-qlist", site.Events.Select(e => e.Title).Concat(site.Events.Select(e => e.Place))));
            sb.Append("<label id=\"ez-showw\">Show<select id=\"ez-eshow\" onchange=\"AdminEventsJs.list(1)\">" + Ui.Opts(new[] { ("up", "Coming up"), ("past", "Past events"), ("draft", "Drafts"), ("all", "Every event") }, "up") + "</select></label>");
            sb.Append("<label>Kind<select id=\"ez-ekind\" onchange=\"AdminEventsJs.list(1)\"><option value=\"\">Any kind</option>" + Ui.Opts(Ui.EventKinds.Select(k => (k.Key, k.Name)), string.Empty) + "</select></label></div><div id=\"ez-listbox\"><div class=\"ez-tbl ez-eztbl\" id=\"ez-list\"></div><div id=\"ez-pager\"></div></div><div id=\"ez-calbox\" data-add=\"" + (Access.Can(c.U, AreaKey, 'E') ? "1" : "0") + "\" hidden><div class=\"ez-legend\">" + string.Concat(Ui.EventKinds.Select(k => "<span class=\"ez-k-" + k.Key + "\"><i></i>" + k.Name + "</span>")) + "<span><i style=\"background:none;border:1px dashed var(--ez-ink2)\"></i>Draft</span></div><div id=\"ez-cal\"></div></div>");
            return sb.ToString();
        }

        private static string Chip(SiteData site, Event e, bool week)
        {
            bool full = e.Capacity > 0 && Taken(e) >= e.Capacity;
            string cls = "ez-k-" + e.Kind + (e.Published ? string.Empty : " ez-dr");
            string tip = Ui.Attr(e.Title + " · " + Ui.HmText(site, e.Start) + " – " + Ui.HmText(site, e.End) + (e.Published ? string.Empty : " · Draft") + (full ? " · Full" : string.Empty));
            if (week)
            {
                return "<button type=\"button\" class=\"ez-wc " + cls + "\" title=\"" + tip + "\" onclick=\"AdminEventsJs.edit('" + Ui.Attr(e.Id) + "')\"><b>" + Ui.Enc(e.Title) + "</b><small>" + Ui.HmText(site, e.Start) + " &ndash; " + Ui.HmText(site, e.End) + "</small><small>" + Ui.Enc(e.Place) + "</small><small>" + (e.Capacity > 0 ? Taken(e) + " of " + e.Capacity + " places" + (full ? " &middot; full" : string.Empty) : "Drop in") + (e.Published ? string.Empty : " &middot; draft") + "</small></button>";
            }
            return "<button type=\"button\" class=\"ez-ce " + cls + "\" title=\"" + tip + "\" onclick=\"AdminEventsJs.edit('" + Ui.Attr(e.Id) + "')\"><i>" + Ui.HmText(site, e.Start) + "</i>" + Ui.Enc(e.Title) + (full ? "<em>full</em>" : string.Empty) + "</button>";
        }

        public async Task<ApiResponse> Cal()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminEventsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return response;
            }
            SiteData site = await Store.LoadSite();
            DateTime today = Store.Today(site);
            bool week = GetDataValue("cview") == "week";
            bool canE = Access.Can(c.U, AreaKey, 'E');
            string kind = GetDataValue("ekind");
            int step = int.TryParse(GetDataValue("cstep"), out int sv) ? Math.Clamp(sv, -1, 1) : 0;
            DateTime at = Ui.ParseDay(GetDataValue("cdate")) ?? today;
            List<Event> evs = site.Events.Where(e => kind == string.Empty || e.Kind == kind).ToList();
            StringBuilder sb = new StringBuilder();
            string label;
            string anchor;
            if (week)
            {
                DateTime start = at.Date.AddDays(-(int)at.DayOfWeek).AddDays(7 * step);
                DateTime end = start.AddDays(6);
                anchor = Ui.Ymd(start);
                label = start.Month == end.Month ? start.ToString("MMM d", Ui.Inv) + " – " + end.ToString("d, yyyy", Ui.Inv) : start.ToString("MMM d", Ui.Inv) + " – " + end.ToString("MMM d, yyyy", Ui.Inv);
                sb.Append("<div class=\"ez-week\">");
                for (int i = 0; i < 7; i++)
                {
                    DateTime d = start.AddDays(i);
                    List<Event> day = evs.Where(e => e.Date.Date == d).OrderBy(e => e.Start).ToList();
                    sb.Append("<div class=\"ez-wd" + (d == today ? " ez-now" : string.Empty) + "\"><header><span>" + d.ToString("ddd", Ui.Inv) + "</span><b>" + d.Day + "</b></header>");
                    foreach (Event e in day)
                    {
                        sb.Append(Chip(site, e, true));
                    }
                    if (day.Count == 0)
                    {
                        sb.Append("<span class=\"ez-wnone\">Nothing planned</span>");
                    }
                    if (canE)
                    {
                        sb.Append("<button type=\"button\" class=\"ez-wadd\" onclick=\"AdminEventsJs.newOn('" + Ui.Ymd(d) + "')\">+ Add</button>");
                    }
                    sb.Append("</div>");
                }
                sb.Append("</div>");
            }
            else
            {
                DateTime first = new DateTime(at.Year, at.Month, 1).AddMonths(step);
                DateTime start = first.AddDays(-(int)first.DayOfWeek);
                DateTime last = first.AddMonths(1).AddDays(-1);
                int weeks = ((last - start).Days / 7) + 1;
                anchor = Ui.Ymd(first);
                label = first.ToString("MMMM yyyy", Ui.Inv);
                sb.Append("<div class=\"ez-month\"><div class=\"ez-mhead\">" + string.Concat(new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" }.Select(x => "<span>" + x + "</span>")) + "</div>");
                StringBuilder ag = new StringBuilder();
                for (int w = 0; w < weeks; w++)
                {
                    sb.Append("<div class=\"ez-mrow\">");
                    for (int i = 0; i < 7; i++)
                    {
                        DateTime d = start.AddDays(w * 7 + i);
                        List<Event> day = evs.Where(e => e.Date.Date == d).OrderBy(e => e.Start).ToList();
                        sb.Append("<div class=\"ez-cd" + (d.Month != first.Month ? " ez-oth" : string.Empty) + (d == today ? " ez-now" : string.Empty) + (canE ? " ez-add" : string.Empty) + "\" onclick=\"AdminEventsJs.dayClick(event,'" + Ui.Ymd(d) + "')\"" + (canE ? " title=\"Add an event on " + d.ToString("MMM d", Ui.Inv) + "\"" : string.Empty) + "><span class=\"ez-cdn\">" + d.Day + "</span><div class=\"ez-ces\">" + string.Concat(day.Select(e => Chip(site, e, false))) + "</div></div>");
                        if (d.Month == first.Month && day.Count > 0)
                        {
                            ag.Append("<h3>" + d.ToString("dddd, MMM d", Ui.Inv) + "</h3>" + string.Concat(day.Select(e => Chip(site, e, true))));
                        }
                    }
                    sb.Append("</div>");
                }
                sb.Append("</div><div class=\"ez-agenda\">" + (ag.Length > 0 ? ag.ToString() : "<p class=\"ez-hint\">No events this month.</p>") + "</div>");
            }
            response.SetElementContents("ez-cal", sb.ToString());
            response.ExecuteScript("AdminEventsJs.calSet('" + anchor + "','" + Ui.Js(label) + "');");
            return response;
        }

        public async Task<ApiResponse> List()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminEventsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return response;
            }
            SiteData site = await Store.LoadSite();
            DateTime today = Store.Today(site);
            string term = Ui.Clip(GetDataValue("eq"), 40);
            string show = GetDataValue("eshow");
            string kind = GetDataValue("ekind");
            IEnumerable<Event> q = site.Events.Where(e => (kind == string.Empty || e.Kind == kind) && show switch
            {
                "past" => e.Date.Date < today,
                "draft" => !e.Published,
                "all" => true,
                _ => e.Date.Date >= today
            });
            if (term != string.Empty)
            {
                q = q.Where(e => (e.Title + " " + e.Place + " " + e.Id).Contains(term, StringComparison.OrdinalIgnoreCase));
            }
            List<Event> all = (show == "past" ? q.OrderByDescending(e => e.Date) : q.OrderBy(e => e.Date)).ThenBy(e => e.Start).ToList();
            int page = Ui.PageOf(GetDataValue("pg"), Size, all.Count);
            StringBuilder sb = new StringBuilder(Head(new[] { ("Date", string.Empty), ("Event", string.Empty), ("Place", string.Empty), ("Reservations", string.Empty), ("Website", string.Empty), (string.Empty, string.Empty) }));
            if (all.Count == 0)
            {
                sb.Append("<div class=\"ez-tr\"><span class=\"ez-none ez-wide\">No events match.</span></div>");
            }
            foreach (Event e in all.Skip((page - 1) * Size).Take(Size))
            {
                int taken = Taken(e);
                string seats = e.Capacity > 0 ? "<b>" + taken + "</b> of " + e.Capacity + "<span class=\"ez-meter\"><i" + (taken >= e.Capacity ? " class=\"ez-full\"" : string.Empty) + " style=\"width:" + Math.Min(100, taken * 100 / e.Capacity) + "%\"></i></span>" : "<i class=\"ez-hint\">Drop in</i>";
                sb.Append("<div class=\"ez-tr" + (e.Date.Date < today ? " ez-past" : string.Empty) + "\"><span>" + Tile(e) + "</span><span class=\"ez-wide\"><b>" + Ui.Enc(e.Title) + "</b><small>" + Ui.NameOf(Ui.EventKinds, e.Kind) + " &middot; " + Ui.HmText(site, e.Start) + " &ndash; " + Ui.HmText(site, e.End) + "</small></span><span data-l=\"Place\" title=\"" + Ui.Attr(e.Place) + "\">" + Ui.Enc(e.Place) + "</span><span data-l=\"Reservations\">" + seats + "</span><span data-l=\"Website\">" + (e.Published ? "<span class=\"ez-live\">Live</span>" : "<span class=\"ez-draft\">Draft</span>") + "</span><span><button type=\"button\" class=\"ez-btn ez-btn-sm ez-btn-ghost\" onclick=\"AdminEventsJs.edit('" + Ui.Attr(e.Id) + "')\">" + (Access.Can(c.U, AreaKey, 'E') ? "Edit" : "View") + "</button></span></div>");
            }
            response.SetElementContents("ez-list", sb.ToString());
            response.SetElementContents("ez-pager", Ui.Pager("ez", page, Size, all.Count, "event", "events", "AdminEventsJs.list"));
            return response;
        }

        private static string GuestsHtml(SiteData site, Event e, bool canE)
        {
            int taken = Taken(e);
            int arrived = e.Rsvps.Where(r => r.Arrived).Sum(r => r.Guests);
            StringBuilder sb = new StringBuilder("<div class=\"ez-gsum\"><span><b>" + e.Rsvps.Count + "</b>" + (e.Rsvps.Count == 1 ? "reservation" : "reservations") + "</span><span><b>" + taken + "</b>" + (taken == 1 ? "place" : "places") + (e.Capacity > 0 ? " of " + e.Capacity : string.Empty) + "</span><span><b id=\"ez-arr\">" + arrived + "</b>checked in</span></div>");
            if (e.Rsvps.Count == 0)
            {
                sb.Append("<p class=\"ez-hint\">" + (e.Capacity > 0 ? "No reservations yet." : "This is a drop-in event. Set the number of places to take reservations.") + "</p>");
                return sb.ToString();
            }
            sb.Append("<div class=\"ez-guests\">");
            foreach (Rsvp r in e.Rsvps.OrderBy(r => r.Name))
            {
                sb.Append("<div class=\"ez-guest\"><span><b>" + Ui.Enc(r.Name) + "</b><small>Reserved " + Ui.Time(r.Created) + "</small></span><span><a href=\"mailto:" + Ui.Attr(r.Email) + "\">" + Ui.Enc(r.Email) + "</a><small>" + Ui.Enc(r.Phone) + "</small></span><span>" + Ui.Plural(r.Guests, "place", "places") + "</span><label class=\"ez-chk\"><input type=\"checkbox\"" + (r.Arrived ? " checked" : string.Empty) + (canE ? " onchange=\"AdminEventsJs.arrive('" + Ui.Attr(r.Id) + "',this)\"" : " disabled") + "> Arrived</label>" + (canE ? "<button type=\"button\" class=\"ez-ibtn\" aria-label=\"Cancel reservation\" onclick=\"AdminEventsJs.drop('" + Ui.Attr(r.Id) + "','" + Ui.Js(r.Name) + "')\">&times;</button>" : "<span></span>") + "</div>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        public async Task<ApiResponse> Form()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminEventsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return await Refuse(response, c, "event", GetDataValue("eid"), "You don't have access to events.");
            }
            SiteData site = await Store.LoadSite();
            DateTime today = Store.Today(site);
            Event? found = site.Events.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("eid"), 20));
            bool canE = Access.Can(c.U, AreaKey, 'E');
            if (found == null && !canE)
            {
                return await Refuse(response, c, "event", "new", "You can look but not add events.");
            }
            DateTime? preset = Ui.ParseDay(GetDataValue("eday"));
            Event e = found ?? new Event { Kind = "open", Date = preset ?? today.AddDays(14), Start = "11:00", End = "15:00", Place = site.Settings.Name + ", " + site.Settings.Address, Published = false };
            string ro = canE ? string.Empty : " disabled";
            StringBuilder sb = new StringBuilder("<h2>" + (found == null ? "Add an event" : Ui.Enc(e.Title)) + "</h2><div id=\"ez-edata\" data-id=\"" + Ui.Attr(found?.Id ?? string.Empty) + "\" data-pic=\"" + Ui.Attr(e.Image) + "\" data-pthumb=\"" + Ui.Attr(e.Image != string.Empty ? site.Media.FirstOrDefault(m => m.Full == e.Image)?.Thumb ?? e.Image : string.Empty) + "\" hidden></div>");
            sb.Append("<div class=\"ez-tabs\" role=\"tablist\"><button type=\"button\" class=\"ez-tab ez-act\" data-p=\"det\" onclick=\"AdminEventsJs.pane('det')\">Details</button><button type=\"button\" class=\"ez-tab\" data-p=\"pic\" onclick=\"AdminEventsJs.pane('pic')\">Picture</button>" + (found != null ? "<button type=\"button\" class=\"ez-tab\" data-p=\"gue\" onclick=\"AdminEventsJs.pane('gue')\">Guests<em>" + e.Rsvps.Count + "</em></button>" : string.Empty) + "</div>");
            sb.Append("<fieldset class=\"ez-form\" style=\"border:0;padding:0;margin:0;min-width:0\"" + ro + "><div class=\"ez-panes\">");
            sb.Append("<div data-p=\"det\" class=\"ez-act\"><div class=\"ez-f2\">" + F("etitle", "Title", e.Title, 80) + "<label class=\"ez-field\"><span>Kind</span><select id=\"ez-ekind2\">" + Ui.Opts(Ui.EventKinds.Select(k => (k.Key, k.Name)), e.Kind) + "</select></label></div>");
            sb.Append("<div class=\"ez-f4\">" + F("edate", "Date", Ui.Ymd(e.Date), 10, "date") + F("estart", "Starts", e.Start, 5, "time") + F("eend", "Ends", e.End, 5, "time") + F("ecap", "Places (0 = drop in)", e.Capacity.ToString(Ui.Inv), 4, "text", " inputmode=\"numeric\"") + "</div>");
            sb.Append(F("eplace", "Place", e.Place, 120));
            sb.Append("<label class=\"ez-field\"><span>Description</span><textarea id=\"ez-etext\" rows=\"5\" maxlength=\"1200\">" + Ui.Enc(e.Text) + "</textarea></label>");
            sb.Append("<div class=\"ez-checks\"><label class=\"ez-chk\"><input type=\"checkbox\" id=\"ez-epub\"" + (e.Published ? " checked" : string.Empty) + "> Show on the website</label></div>" + (found != null && e.Rsvps.Count > 0 ? "<p class=\"ez-hint\">" + Ui.Plural(Taken(e), "place is", "places are") + " already reserved.</p>" : string.Empty) + "</div>");
            sb.Append("<div data-p=\"pic\"><div class=\"ez-cover\" id=\"ez-cover\"></div>" + (canE ? "<div class=\"ez-picbtns\"><button type=\"button\" class=\"ez-btn ez-btn-sm\" onclick=\"AdminEventsJs.choosePic()\">Choose from Media</button><button type=\"button\" class=\"ez-btn ez-btn-sm ez-btn-ghost\" id=\"ez-nopic\" onclick=\"AdminEventsJs.noPic()\">Remove picture</button></div><p class=\"ez-hint\">Wide pictures work best. The website shows them at 2 to 1.</p>" : string.Empty));
            sb.Append("</div>");
            if (found != null)
            {
                sb.Append("<div data-p=\"gue\" id=\"ez-gbox\">" + GuestsHtml(site, e, canE) + "</div>");
            }
            sb.Append("</div></fieldset><em class=\"ez-err\" id=\"ez-e-save\" role=\"alert\"></em><div class=\"ez-mfoot\">");
            if (found != null && canE)
            {
                sb.Append("<button type=\"button\" class=\"ez-btn ez-btn-danger ez-left\" onclick=\"AdminEventsJs.remove('" + Ui.Js(found.Title) + "'," + found.Rsvps.Count + ")\">Delete</button>");
            }
            if (found != null && found.Published && found.Date.Date >= today)
            {
                sb.Append("<a class=\"ez-btn ez-btn-ghost\" href=\"Events#" + Uri.EscapeDataString(found.Id) + "\" target=\"_blank\" rel=\"noopener\">Open on the website</a>");
            }
            sb.Append("<button type=\"button\" class=\"ez-btn ez-btn-ghost\" onclick=\"AdminEventsJs.closeModal()\">" + (canE ? "Cancel" : "Close") + "</button>" + (canE ? "<button type=\"button\" class=\"ez-btn ez-btn-accent\" onclick=\"AdminEventsJs.save()\">" + (found == null ? "Add event" : "Save") + "</button>" : string.Empty) + "</div>");
            response.SetElementContents("ez-mbody", sb.ToString());
            response.ExecuteScript("AdminEventsJs.opened(" + (GetDataValue("etab") == "gue" ? "'gue'" : "''") + ");");
            return response;
        }

        public async Task<ApiResponse> Save()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminEventsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "event", GetDataValue("eid"), "You can look but not change events.");
            }
            string id = Ui.Clip(GetDataValue("eid"), 20);
            string title = Regex.Replace(Ui.Clip(GetDataValue("etitle"), 80), "\\s+", " ").Trim();
            string kind = Ui.EventKinds.Any(k => k.Key == GetDataValue("ekind2")) ? GetDataValue("ekind2") : string.Empty;
            DateTime? day = Ui.ParseDay(GetDataValue("edate"));
            TimeSpan? start = Store.Hm(GetDataValue("estart"));
            TimeSpan? end = Store.Hm(GetDataValue("eend"));
            string place = Regex.Replace(Ui.Clip(GetDataValue("eplace"), 120), "\\s+", " ").Trim();
            string text = Ui.Clip(GetDataValue("etext"), 1200).Replace("\r", string.Empty).Trim();
            bool capOk = int.TryParse(GetDataValue("ecap").Trim() == string.Empty ? "0" : GetDataValue("ecap").Trim(), NumberStyles.Integer, Ui.Inv, out int cap) && cap >= 0 && cap <= 2000;
            string pic = GetDataValue("epic");
            string err = string.Empty;
            if (title.Length < 3)
            {
                err = "Enter a title.";
            }
            else if (kind == string.Empty)
            {
                err = "Choose the kind of event.";
            }
            else if (day == null || day.Value < DateTime.UtcNow.Date.AddYears(-2) || day.Value > DateTime.UtcNow.Date.AddYears(2))
            {
                err = "Check the date.";
            }
            else if (start == null || end == null || end.Value <= start.Value)
            {
                err = "Check the times. The event must end after it starts.";
            }
            else if (place.Length < 3)
            {
                err = "Enter where the event takes place.";
            }
            else if (!capOk)
            {
                err = "Places must be a whole number from 0 to 2000.";
            }
            else if (pic != string.Empty && (!Ui.SafeImage(pic) || Ui.IsVideo(pic)))
            {
                err = "Choose the picture again.";
            }
            if (err != string.Empty)
            {
                response.ExecuteScript("AdminEventsJs.fail('" + Ui.Js(err) + "');");
                return response;
            }
            bool isNew = false;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Event? e = site.Events.FirstOrDefault(x => x.Id == id);
                if (e != null && cap > 0 && cap < Taken(e))
                {
                    err = Taken(e) + " places are already reserved. Set at least " + Taken(e) + ", or cancel some reservations first.";
                }
                else
                {
                    if (e == null)
                    {
                        isNew = true;
                        e = new Event { Id = Store.NextId(site.Events.Select(x => x.Id), "E-", 600) };
                        site.Events.Add(e);
                    }
                    e.Title = title;
                    e.Kind = kind;
                    e.Date = day!.Value;
                    e.Start = start!.Value.ToString("hh\\:mm", Ui.Inv);
                    e.End = end!.Value.ToString("hh\\:mm", Ui.Inv);
                    e.Place = place;
                    e.Text = text;
                    e.Capacity = cap;
                    e.Image = pic;
                    e.Published = GetDataValue("epub") == "1";
                    id = e.Id;
                    await Store.SaveSite(site);
                }
            }
            if (err != string.Empty)
            {
                response.ExecuteScript("AdminEventsJs.fail('" + Ui.Js(err) + "');");
                return response;
            }
            await Store.Log(c.U.Id, isNew ? "add event" : "edit event", title, true);
            response.ExecuteScript("AdminEventsJs.saved('" + Ui.Js(id) + "'," + (isNew ? "true" : "false") + ");");
            return response;
        }

        public async Task<ApiResponse> Arrive()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminEventsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "check in", GetDataValue("eid"), "You can look but not check guests in.");
            }
            int n = 0;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Event? e = site.Events.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("eid"), 20));
                Rsvp? r = e?.Rsvps.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("rid"), 20));
                if (e == null || r == null)
                {
                    return response;
                }
                r.Arrived = GetDataValue("on") == "1";
                n = e.Rsvps.Where(x => x.Arrived).Sum(x => x.Guests);
                await Store.SaveSite(site);
            }
            response.ExecuteScript("AdminEventsJs.arrived(" + n + ");");
            return response;
        }

        public async Task<ApiResponse> DropRsvp()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminEventsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "cancel reservation", GetDataValue("eid"), "You can look but not change reservations.");
            }
            string html = string.Empty;
            string name = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Event? e = site.Events.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("eid"), 20));
                Rsvp? r = e?.Rsvps.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("rid"), 20));
                if (e == null || r == null)
                {
                    return response;
                }
                e.Rsvps.Remove(r);
                name = e.Title + " · " + r.Name;
                await Store.SaveSite(site);
                html = GuestsHtml(site, e, true);
            }
            await Store.Log(c.U.Id, "cancel reservation", name, true);
            response.SetElementContents("ez-gbox", html);
            response.ExecuteScript("AdminEventsJs.list(1); AdminEventsJs.toast('Reservation cancelled.');");
            return response;
        }

        public async Task<ApiResponse> Delete()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminEventsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "delete event", GetDataValue("eid"), "You can look but not delete events.");
            }
            string name = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Event? e = site.Events.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("eid"), 20));
                if (e == null)
                {
                    return response;
                }
                name = e.Title;
                site.Events.Remove(e);
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "delete event", name, true);
            response.ExecuteScript("AdminEventsJs.deleted();");
            return response;
        }
    }
}
