using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DogShelter.Core;
using DogShelter.Models;
using SkyNet;

namespace DogShelter.codes
{
    public class AdminVolunteers : WebPage
    {
        private const string AreaKey = "volunteers";

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle(site.Settings.Name + " admin | Volunteers");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText.Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--vz-accent:" + site.Settings.Accent : string.Empty).Replace("{plhd_name}", Ui.Enc(site.Settings.Name));
        }

        private static void Chrome(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("vz-menu", Auth.Menu("vz", "AdminVolunteers", c, site));
            response.SetElementContents("vz-who", Auth.Who("vz", c));
        }

        private static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript("AdminVolunteersJs.toast('" + Ui.Js(message) + "');");
            return response;
        }

        private static string NoAccess(string title)
        {
            return "<div class=\"vz-ptop\"><div><h1>" + Ui.Enc(title) + "</h1></div></div><div class=\"vz-panel\"><p>You don't have access to this area. Ask the shelter manager to give you access in Users.</p></div>";
        }

        public async Task<ApiResponse> SignOut()
        {
            ApiResponse response = new ApiResponse();
            await Auth.Close(GetDataValue("tok"));
            response.ExecuteScript("AdminVolunteersJs.out();");
            return response;
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminVolunteersJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Chrome(response, c, site);
            if (AreaKey != string.Empty && !Access.Can(c.U, AreaKey, 'V'))
            {
                await Store.Log(c.U.Id, "view", AreaKey, false);
                response.SetElementContents("vz-main", NoAccess("Volunteers"));
                return response;
            }
            response.SetElementContents("vz-main", await MainHtml(c, site, GetDataValue("arg")));
            response.ExecuteScript("AdminVolunteersJs.loaded();");
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
            StringBuilder sb = new StringBuilder("<div class=\"vz-tr vz-th\">");
            foreach ((string n, string cls) in cols)
            {
                sb.Append("<span" + (cls != string.Empty ? " class=\"" + cls + "\"" : string.Empty) + ">" + n + "</span>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private static string NotesHtml(List<Note> notes, Dictionary<string, Staff> staff, bool canE, string hint)
        {
            StringBuilder sb = new StringBuilder("<h3>Notes</h3><div class=\"vz-notes\">");
            if (notes.Count == 0)
            {
                sb.Append("<p class=\"vz-hint\">No notes yet.</p>");
            }
            foreach (Note n in notes.OrderByDescending(n => n.When))
            {
                sb.Append("<div class=\"vz-note\">" + Ui.Enc(n.Text) + "<small>" + Ui.Enc(Ui.StaffName(staff, n.By)) + " &middot; " + Ui.Time(n.When) + "</small></div>");
            }
            sb.Append("</div>");
            if (canE)
            {
                sb.Append("<div class=\"vz-form\" style=\"margin-top:10px\"><label class=\"vz-field\"><span>Add a note</span><textarea id=\"vz-enote\" rows=\"2\" maxlength=\"500\" placeholder=\"" + Ui.Attr(hint) + "\"></textarea></label><div><button type=\"button\" class=\"vz-btn vz-btn-sm\" onclick=\"AdminVolunteersJs.note()\">Save note</button></div></div>");
            }
            return sb.ToString();
        }

        private static string F(string id, string label, string value, int max, string type = "text", string extra = "")
        {
            return "<label class=\"vz-field\"><span>" + label + "</span><input type=\"" + type + "\" id=\"vz-" + id + "\" maxlength=\"" + max + "\" value=\"" + Ui.Attr(value) + "\"" + extra + "></label>";
        }

        private const int Size = 20;

        private static readonly string[] Current = { "new", "approved", "active" };

        private static string Short(string t)
        {
            return t switch
            {
                "wd-am" => "Wkday AM",
                "wd-pm" => "Wkday PM",
                "wd-ev" => "Wkday eve",
                "we-am" => "Wkend AM",
                "we-pm" => "Wkend PM",
                _ => t
            };
        }

        private static string Hrs(decimal h)
        {
            return h.ToString("0.#", Ui.Inv) + (h == 1 ? " hr" : " hrs");
        }

        private static string Summary(SiteData site)
        {
            DateTime today = Store.Today(site);
            DateTime month = new DateTime(today.Year, today.Month, 1);
            decimal hm = site.Volunteers.SelectMany(v => v.Hours).Where(h => h.When >= month && h.When <= today).Sum(h => h.Hours);
            decimal hy = site.Volunteers.SelectMany(v => v.Hours).Where(h => h.When.Year == today.Year && h.When <= today).Sum(h => h.Hours);
            return "<div><b>" + site.Volunteers.Count(v => v.Status == "active") + "</b><span>Active volunteers</span></div><div><b>" + site.Volunteers.Count(v => v.Status == "new") + "</b><span>New sign-ups</span></div><div><b>" + hm.ToString("0.#", Ui.Inv) + "</b><span>Hours in " + today.ToString("MMMM", Ui.Inv) + "</span></div><div><b>" + hy.ToString("#,0.#", Ui.Inv) + "</b><span>Hours in " + today.Year + "</span></div>";
        }

        private static async Task<string> MainHtml(Ctx c, SiteData site, string arg)
        {
            string st = Ui.VolStatuses.Any(s => s.Key == arg) ? arg : string.Empty;
            StringBuilder sb = new StringBuilder("<div class=\"vz-ptop\"><div><h1>Volunteers</h1><p>Sign-ups from the website with the jobs and times they offered. Log hours after each shift.</p></div><div class=\"vz-acts\">" + (Access.Can(c.U, AreaKey, 'E') ? "<button type=\"button\" class=\"vz-btn vz-btn-accent\" onclick=\"AdminVolunteersJs.form('')\">Add a volunteer</button>" : string.Empty) + "</div></div><div class=\"vz-vsum\" id=\"vz-vsum\">" + Summary(site) + "</div>");
            sb.Append("<div class=\"vz-bar\"><label>Search<input type=\"search\" id=\"vz-q\" list=\"vz-qlist\" placeholder=\"Name, email or phone\" maxlength=\"40\" oninput=\"AdminVolunteersJs.search()\"></label>" + Ui.DataList("vz-qlist", site.Volunteers.Select(v => v.Name)));
            sb.Append("<label>Status<select id=\"vz-vst\" onchange=\"AdminVolunteersJs.list(1)\"><option value=\"\">Current</option>" + Ui.Opts(Ui.VolStatuses.Select(s => (s.Key, s.Name)), st) + "<option value=\"all\">Everyone</option></select></label>");
            sb.Append("<label>Helps with<select id=\"vz-vrole\" onchange=\"AdminVolunteersJs.list(1)\"><option value=\"\">Any job</option>" + Ui.Opts(Ui.Roles.Select(s => (s.Key, s.Name)), string.Empty) + "</select></label>");
            sb.Append("<label>Free<select id=\"vz-vtime\" onchange=\"AdminVolunteersJs.list(1)\"><option value=\"\">Any time</option>" + Ui.Opts(Ui.Times.Select(s => (s.Key, s.Name)), string.Empty) + "</select></label></div><div class=\"vz-tbl vz-vztbl\" id=\"vz-list\"></div><div id=\"vz-pager\"></div>");
            return sb.ToString();
        }

        public async Task<ApiResponse> List()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminVolunteersJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return response;
            }
            SiteData site = await Store.LoadSite();
            string term = Ui.Clip(GetDataValue("vq"), 40);
            string st = GetDataValue("vst");
            string role = GetDataValue("vrole");
            string time = GetDataValue("vtime");
            IEnumerable<VolunteerInfo> q = site.Volunteers.Where(v => (st == "all" || (st == string.Empty ? Current.Contains(v.Status) : v.Status == st)) && (role == string.Empty || v.Roles.Contains(role)) && (time == string.Empty || v.Times.Contains(time)));
            if (term != string.Empty)
            {
                q = q.Where(v => (v.Name + " " + v.Email + " " + v.Phone + " " + v.Id).Contains(term, StringComparison.OrdinalIgnoreCase));
            }
            List<VolunteerInfo> all = q.OrderBy(v => v.Status == "new" ? 0 : 1).ThenBy(v => v.Name).ToList();
            int page = Ui.PageOf(GetDataValue("pg"), Size, all.Count);
            StringBuilder sb = new StringBuilder(Head(new[] { ("Volunteer", string.Empty), ("Helps with", string.Empty), ("Free", string.Empty), ("Hours", "vz-num"), ("Status", string.Empty), ("Signed up", string.Empty), (string.Empty, string.Empty) }));
            if (all.Count == 0)
            {
                sb.Append("<div class=\"vz-tr\"><span class=\"vz-none vz-wide\">No volunteers match.</span></div>");
            }
            foreach (VolunteerInfo v in all.Skip((page - 1) * Size).Take(Size))
            {
                string roles = string.Join(", ", Ui.Roles.Where(r => v.Roles.Contains(r.Key)).Select(r => r.Name));
                HourLog? last = v.Hours.OrderByDescending(h => h.When).FirstOrDefault();
                sb.Append("<div class=\"vz-tr" + (v.Status == "new" ? " vz-fresh" : string.Empty) + "\"><span class=\"vz-wide\"><b>" + Ui.Enc(v.Name) + (v.Adult ? string.Empty : " <span class=\"vz-tag\">Under 18</span>") + "</b><small>" + Ui.Enc(v.Email) + " &middot; " + Ui.Enc(v.Phone) + "</small></span><span data-l=\"Helps with\" title=\"" + Ui.Attr(roles) + "\">" + Ui.Enc(roles) + "</span><span data-l=\"Free\">" + Ui.Enc(string.Join(", ", Ui.Times.Where(t => v.Times.Contains(t.Key)).Select(t => Short(t.Key)))) + "</span><span data-l=\"Hours\" class=\"vz-num\">" + v.Hours.Sum(h => h.Hours).ToString("0.#", Ui.Inv) + (last != null ? "<small>" + last.When.ToString("MMM d", Ui.Inv) + "</small>" : string.Empty) + "</span><span data-l=\"Status\">" + Ui.Pill("vz", Ui.VolStatuses, v.Status) + "</span><span data-l=\"Signed up\">" + Ui.Time(v.Created) + "</span><span><button type=\"button\" class=\"vz-btn vz-btn-sm vz-btn-ghost\" onclick=\"AdminVolunteersJs.open('" + Ui.Attr(v.Id) + "')\">Open</button></span></div>");
            }
            response.SetElementContents("vz-list", sb.ToString());
            response.SetElementContents("vz-pager", Ui.Pager("vz", page, Size, all.Count, "volunteer", "volunteers", "AdminVolunteersJs.list"));
            response.SetElementContents("vz-vsum", Summary(site));
            return response;
        }

        public async Task<ApiResponse> Open()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminVolunteersJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return await Refuse(response, c, "volunteer", GetDataValue("vid"), "You don't have access to volunteers.");
            }
            SiteData site = await Store.LoadSite();
            DateTime today = Store.Today(site);
            VolunteerInfo? v = site.Volunteers.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("vid"), 20));
            if (v == null)
            {
                response.ExecuteScript("AdminVolunteersJs.toast('That volunteer no longer exists.');");
                return response;
            }
            Dictionary<string, Staff> staff = Ui.StaffMap(await Store.LoadStaff());
            bool canE = Access.Can(c.U, AreaKey, 'E');
            decimal total = v.Hours.Sum(h => h.Hours);
            StringBuilder sb = new StringBuilder("<h2>" + Ui.Enc(v.Name) + " <small style=\"font-family:var(--vz-text);font-size:14px;color:var(--vz-ink2)\">" + Ui.Enc(v.Id) + "</small></h2><dl class=\"vz-dl\"><dt>Status</dt><dd>" + Ui.Pill("vz", Ui.VolStatuses, v.Status) + "</dd><dt>Signed up</dt><dd>" + Ui.Time(v.Created, "full") + "</dd><dt>Email</dt><dd><a href=\"mailto:" + Ui.Attr(v.Email) + "\">" + Ui.Enc(v.Email) + "</a></dd><dt>Phone</dt><dd><a href=\"tel:" + Ui.Attr(Ui.Digits(v.Phone)) + "\">" + Ui.Enc(v.Phone) + "</a></dd><dt>Age</dt><dd>" + (v.Adult ? "18 or older" : "Under 18, comes with a parent or guardian") + "</dd><dt>Helps with</dt><dd>" + Ui.Enc(string.Join(", ", Ui.Roles.Where(r => v.Roles.Contains(r.Key)).Select(r => r.Name))) + "</dd><dt>Free</dt><dd>" + Ui.Enc(string.Join(", ", Ui.Times.Where(t => v.Times.Contains(t.Key)).Select(t => t.Name))) + "</dd><dt>Hours given</dt><dd><b>" + Hrs(total) + "</b>" + (v.Hours.Count > 0 ? " over " + Ui.Plural(v.Hours.Count, "shift", "shifts") : string.Empty) + "</dd></dl>");
            if (v.Message != string.Empty)
            {
                sb.Append("<h3>Message</h3><div class=\"vz-lmsg\">" + Ui.Enc(v.Message) + "</div>");
            }
            if (canE)
            {
                sb.Append("<h3>Status</h3><div class=\"vz-stbtns\">");
                foreach ((string k, string n) in Ui.VolStatuses)
                {
                    sb.Append("<button type=\"button\" class=\"vz-chip" + (k == v.Status ? " vz-act" : string.Empty) + "\" onclick=\"AdminVolunteersJs.status('" + k + "')\"" + (k == v.Status ? " aria-pressed=\"true\"" : string.Empty) + ">" + n + "</button>");
                }
                sb.Append("</div>");
            }
            sb.Append("<h3>Hours</h3>");
            if (canE && v.Status is "approved" or "active")
            {
                sb.Append("<div class=\"vz-logh\">" + F("hday", "Date", Ui.Ymd(today), 10, "date") + F("hnum", "Hours", string.Empty, 4, "text", " inputmode=\"decimal\" placeholder=\"2.5\"") + F("hnote", "What they did", string.Empty, 120, "text", " placeholder=\"Morning walks\"") + "<button type=\"button\" class=\"vz-btn vz-btn-sm\" style=\"min-height:44px\" onclick=\"AdminVolunteersJs.logHours()\">Log hours</button></div><em class=\"vz-err\" id=\"vz-e-hours\" role=\"alert\"></em>");
            }
            else if (canE)
            {
                sb.Append("<p class=\"vz-hint\">Approve this volunteer to log hours.</p>");
            }
            if (v.Hours.Count == 0)
            {
                sb.Append("<p class=\"vz-hint\">No hours logged yet.</p>");
            }
            else
            {
                sb.Append("<div class=\"vz-hours\">");
                List<HourLog> rows = v.Hours.Select((h, i) => (h, i)).OrderByDescending(x => x.h.When).Take(30).Select(x => x.h).ToList();
                foreach (HourLog h in rows)
                {
                    int i = v.Hours.IndexOf(h);
                    sb.Append("<div><span>" + h.When.ToString("ddd, MMM d", Ui.Inv) + "</span><b>" + Hrs(h.Hours) + "</b><span title=\"" + Ui.Attr(Ui.StaffName(staff, h.By)) + "\">" + Ui.Enc(h.Note) + "</span>" + (canE ? "<button type=\"button\" class=\"vz-ibtn\" aria-label=\"Remove entry\" onclick=\"AdminVolunteersJs.dropHours(" + i + ")\">&times;</button>" : "<span></span>") + "</div>");
                }
                sb.Append("</div>" + (v.Hours.Count > 30 ? "<p class=\"vz-hint\">Showing the latest 30 of " + v.Hours.Count + " entries.</p>" : string.Empty));
            }
            sb.Append(NotesHtml(v.Notes, staff, canE, "Great with shy dogs. Wants to try transport."));
            sb.Append("<div class=\"vz-mfoot\">" + (canE ? "<button type=\"button\" class=\"vz-btn vz-btn-danger vz-left\" onclick=\"AdminVolunteersJs.remove('" + Ui.Js(v.Name) + "')\">Delete</button><button type=\"button\" class=\"vz-btn vz-btn-ghost\" onclick=\"AdminVolunteersJs.form('" + Ui.Attr(v.Id) + "')\">Edit details</button>" : string.Empty) + "<button type=\"button\" class=\"vz-btn vz-btn-ghost\" onclick=\"AdminVolunteersJs.closeModal()\">Close</button></div>");
            response.SetElementContents("vz-mbody", sb.ToString());
            response.ExecuteScript("AdminVolunteersJs.openModal();");
            return response;
        }

        public async Task<ApiResponse> Form()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminVolunteersJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "volunteer", GetDataValue("vid"), "You can look but not change volunteers.");
            }
            SiteData site = await Store.LoadSite();
            VolunteerInfo? found = site.Volunteers.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("vid"), 20));
            VolunteerInfo v = found ?? new VolunteerInfo { Status = "approved", Adult = true };
            StringBuilder sb = new StringBuilder("<h2>" + (found == null ? "Add a volunteer" : "Edit " + Ui.Enc(v.Name)) + "</h2><p class=\"vz-hint\" style=\"margin:-4px 0 14px\">" + (found == null ? "For people who sign up by phone, at an event or at orientation." : "Details from the sign-up. Hours and notes stay as they are.") + "</p><div class=\"vz-form\">");
            sb.Append("<div class=\"vz-f2\">" + F("fname", "Full name", v.Name, 80) + F("fphone", "Phone", v.Phone, 30, "tel") + "</div><div class=\"vz-f2\">" + F("fmail", "Email", v.Email, 120, "email") + "<label class=\"vz-field\"><span>Status</span><select id=\"vz-fstat\">" + Ui.Opts(Ui.VolStatuses.Select(x => (x.Key, x.Name)), v.Status) + "</select></label></div>");
            sb.Append("<div><span class=\"vz-hint\" style=\"font-weight:700\">Helps with</span><div class=\"vz-opts\" style=\"margin-top:6px\">" + string.Concat(Ui.Roles.Select(r => "<label class=\"vz-chk\"><input type=\"checkbox\" name=\"vz-frole\" value=\"" + r.Key + "\"" + (v.Roles.Contains(r.Key) ? " checked" : string.Empty) + "> " + r.Name + "</label>")) + "</div></div>");
            sb.Append("<div><span class=\"vz-hint\" style=\"font-weight:700\">Free</span><div class=\"vz-opts\" style=\"margin-top:6px\">" + string.Concat(Ui.Times.Select(r => "<label class=\"vz-chk\"><input type=\"checkbox\" name=\"vz-ftime\" value=\"" + r.Key + "\"" + (v.Times.Contains(r.Key) ? " checked" : string.Empty) + "> " + r.Name + "</label>")) + "</div></div>");
            sb.Append("<label class=\"vz-chk\"><input type=\"checkbox\" id=\"vz-fadult\"" + (v.Adult ? " checked" : string.Empty) + "> 18 or older</label><label class=\"vz-field\"><span>About them</span><textarea id=\"vz-fmsg\" rows=\"3\" maxlength=\"1000\" placeholder=\"Experience with dogs, skills, languages\">" + Ui.Enc(v.Message) + "</textarea></label><em class=\"vz-err\" id=\"vz-e-vol\" role=\"alert\"></em></div>");
            sb.Append("<div class=\"vz-mfoot\"><button type=\"button\" class=\"vz-btn vz-btn-ghost\" onclick=\"" + (found != null ? "AdminVolunteersJs.open('" + Ui.Attr(found.Id) + "')" : "AdminVolunteersJs.closeModal()") + "\">Cancel</button><button type=\"button\" class=\"vz-btn vz-btn-accent\" onclick=\"AdminVolunteersJs.saveVol('" + Ui.Attr(found?.Id ?? string.Empty) + "')\">" + (found == null ? "Add volunteer" : "Save") + "</button></div>");
            response.SetElementContents("vz-mbody", sb.ToString());
            response.ExecuteScript("AdminVolunteersJs.openModal();");
            return response;
        }

        public async Task<ApiResponse> SaveVol()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminVolunteersJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "save volunteer", GetDataValue("vid"), "You can look but not change volunteers.");
            }
            string name = Regex.Replace(Ui.Clip(GetDataValue("fname"), 80), "\\s+", " ").Trim();
            string mail = Ui.Clip(GetDataValue("fmail"), 120).Trim();
            string phone = Ui.CleanPhone(GetDataValue("fphone"));
            List<string> roles = GetDataValue("froles").Split(',').Where(x => Ui.Roles.Any(r => r.Key == x)).Distinct().ToList();
            List<string> times = GetDataValue("ftimes").Split(',').Where(x => Ui.Times.Any(r => r.Key == x)).Distinct().ToList();
            string st = Ui.VolStatuses.Any(x => x.Key == GetDataValue("fstat")) ? GetDataValue("fstat") : "approved";
            string err = name.Length < 2 ? "Enter their name." : mail != string.Empty && !Ui.Email(mail) ? "Check the email address." : mail == string.Empty && Ui.Digits(phone).Length < 7 ? "Enter an email address or a phone number." : roles.Count == 0 ? "Choose at least one job." : string.Empty;
            if (err != string.Empty)
            {
                response.ExecuteScript("AdminVolunteersJs.volFail('" + Ui.Js(err) + "');");
                return response;
            }
            string id = Ui.Clip(GetDataValue("vid"), 20);
            bool isNew = false;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                VolunteerInfo? v = site.Volunteers.FirstOrDefault(x => x.Id == id);
                if (v == null)
                {
                    if (mail != string.Empty && site.Volunteers.Any(x => x.Email.Equals(mail, StringComparison.OrdinalIgnoreCase)))
                    {
                        err = "A volunteer with that email is already on the list.";
                    }
                    else
                    {
                        isNew = true;
                        v = new VolunteerInfo { Id = Store.NextId(site.Volunteers.Select(x => x.Id), "V-", 500), Created = DateTime.UtcNow };
                        v.Notes.Add(new Note { When = DateTime.UtcNow, By = c.U.Id, Text = "Added by staff." });
                        site.Volunteers.Add(v);
                    }
                }
                if (v != null && err == string.Empty)
                {
                    v.Name = name;
                    v.Email = mail;
                    v.Phone = phone;
                    v.Roles = roles;
                    v.Times = times;
                    v.Adult = GetDataValue("fadult") == "1";
                    v.Status = st;
                    v.Message = Ui.Clip(GetDataValue("fmsg"), 1000).Trim();
                    id = v.Id;
                    await Store.SaveSite(site);
                }
            }
            if (err != string.Empty)
            {
                response.ExecuteScript("AdminVolunteersJs.volFail('" + Ui.Js(err) + "');");
                return response;
            }
            await Store.Log(c.U.Id, isNew ? "add volunteer" : "edit volunteer", name, true);
            response.ExecuteScript("AdminVolunteersJs.volSaved('" + Ui.Js(id) + "'," + (isNew ? "true" : "false") + ");");
            return response;
        }

        public async Task<ApiResponse> SetStatus()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminVolunteersJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "volunteer status", GetDataValue("vid"), "You can look but not change volunteers.");
            }
            string to = Ui.VolStatuses.Any(s => s.Key == GetDataValue("vst2")) ? GetDataValue("vst2") : string.Empty;
            string name = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                VolunteerInfo? v = site.Volunteers.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("vid"), 20));
                if (v == null || to == string.Empty)
                {
                    return response;
                }
                v.Status = to;
                name = v.Name;
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "volunteer " + to, name, true);
            response.ExecuteScript("AdminVolunteersJs.changed('Marked " + Ui.Js(Ui.NameOf(Ui.VolStatuses, to).ToLowerInvariant()) + ".');");
            return response;
        }

        public async Task<ApiResponse> LogHours()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminVolunteersJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "log hours", GetDataValue("vid"), "You can look but not change volunteers.");
            }
            DateTime? day = Ui.ParseDay(GetDataValue("hday"));
            decimal hours = decimal.TryParse(GetDataValue("hnum").Trim(), NumberStyles.Number, Ui.Inv, out decimal hv) ? Math.Round(hv * 4, MidpointRounding.AwayFromZero) / 4 : 0;
            string err = string.Empty;
            string name = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                DateTime today = Store.Today(site);
                VolunteerInfo? v = site.Volunteers.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("vid"), 20));
                if (v == null)
                {
                    return response;
                }
                if (v.Status is not ("approved" or "active"))
                {
                    err = "Approve this volunteer first.";
                }
                else if (day == null || day.Value > today || day.Value < today.AddYears(-1))
                {
                    err = "Choose a date in the past year, not in the future.";
                }
                else if (hours <= 0 || hours > 12)
                {
                    err = "Enter the hours, from 0.25 to 12.";
                }
                else
                {
                    v.Hours.Add(new HourLog { When = day.Value, Hours = hours, Note = Ui.Clip(GetDataValue("hnote"), 120).Trim(), By = c.U.Id });
                    if (v.Status == "approved")
                    {
                        v.Status = "active";
                    }
                    name = v.Name;
                    await Store.SaveSite(site);
                }
            }
            if (err != string.Empty)
            {
                response.ExecuteScript("document.getElementById('vz-e-hours').textContent = '" + Ui.Js(err) + "';");
                return response;
            }
            await Store.Log(c.U.Id, "log hours", name + " · " + Hrs(hours), true);
            response.ExecuteScript("AdminVolunteersJs.changed('" + Ui.Js(Hrs(hours)) + " logged.');");
            return response;
        }

        public async Task<ApiResponse> DropHours()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminVolunteersJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "remove hours", GetDataValue("vid"), "You can look but not change volunteers.");
            }
            int i = int.TryParse(GetDataValue("hi"), NumberStyles.Integer, Ui.Inv, out int iv) ? iv : -1;
            string name = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                VolunteerInfo? v = site.Volunteers.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("vid"), 20));
                if (v == null || i < 0 || i >= v.Hours.Count)
                {
                    return response;
                }
                v.Hours.RemoveAt(i);
                name = v.Name;
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "remove hours", name, true);
            response.ExecuteScript("AdminVolunteersJs.changed('Entry removed.');");
            return response;
        }

        public async Task<ApiResponse> AddNote()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminVolunteersJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "volunteer note", GetDataValue("vid"), "You can look but not change volunteers.");
            }
            string text = Regex.Replace(Ui.Clip(GetDataValue("enote"), 500), "\\s+", " ").Trim();
            if (text.Length < 2)
            {
                response.ExecuteScript("AdminVolunteersJs.toast('Write the note first.');");
                return response;
            }
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                VolunteerInfo? v = site.Volunteers.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("vid"), 20));
                if (v == null)
                {
                    return response;
                }
                v.Notes.Add(new Note { When = DateTime.UtcNow, By = c.U.Id, Text = text });
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "volunteer note", GetDataValue("vid"), true);
            response.ExecuteScript("AdminVolunteersJs.changed('Note saved.');");
            return response;
        }

        public async Task<ApiResponse> Delete()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminVolunteersJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "delete volunteer", GetDataValue("vid"), "You can look but not change volunteers.");
            }
            string name = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                VolunteerInfo? v = site.Volunteers.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("vid"), 20));
                if (v == null)
                {
                    return response;
                }
                name = v.Name;
                site.Volunteers.Remove(v);
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "delete volunteer", name, true);
            response.ExecuteScript("AdminVolunteersJs.deleted();");
            return response;
        }
    }
}
