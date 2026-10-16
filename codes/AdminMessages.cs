using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DogShelter.Core;
using DogShelter.Models;
using SkyNet;

namespace DogShelter.codes
{
    public class AdminMessages : WebPage
    {
        private const string AreaKey = "messages";

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle(site.Settings.Name + " admin | Messages");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText.Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--ms-accent:" + site.Settings.Accent : string.Empty).Replace("{plhd_name}", Ui.Enc(site.Settings.Name));
        }

        private static void Chrome(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("ms-menu", Auth.Menu("ms", "AdminMessages", c, site));
            response.SetElementContents("ms-who", Auth.Who("ms", c));
        }

        private static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript("AdminMessagesJs.toast('" + Ui.Js(message) + "');");
            return response;
        }

        private static string NoAccess(string title)
        {
            return "<div class=\"ms-ptop\"><div><h1>" + Ui.Enc(title) + "</h1></div></div><div class=\"ms-panel\"><p>You don't have access to this area. Ask the shelter manager to give you access in Users.</p></div>";
        }

        public async Task<ApiResponse> SignOut()
        {
            ApiResponse response = new ApiResponse();
            await Auth.Close(GetDataValue("tok"));
            response.ExecuteScript("AdminMessagesJs.out();");
            return response;
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminMessagesJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Chrome(response, c, site);
            if (AreaKey != string.Empty && !Access.Can(c.U, AreaKey, 'V'))
            {
                await Store.Log(c.U.Id, "view", AreaKey, false);
                response.SetElementContents("ms-main", NoAccess("Messages"));
                return response;
            }
            response.SetElementContents("ms-main", await MainHtml(c, site, GetDataValue("arg")));
            response.ExecuteScript("AdminMessagesJs.loaded();");
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
            StringBuilder sb = new StringBuilder("<div class=\"ms-tr ms-th\">");
            foreach ((string n, string cls) in cols)
            {
                sb.Append("<span" + (cls != string.Empty ? " class=\"" + cls + "\"" : string.Empty) + ">" + n + "</span>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private static string NotesHtml(List<Note> notes, Dictionary<string, Staff> staff, bool canE, string hint)
        {
            StringBuilder sb = new StringBuilder("<h3>Notes</h3><div class=\"ms-notes\">");
            if (notes.Count == 0)
            {
                sb.Append("<p class=\"ms-hint\">No notes yet.</p>");
            }
            foreach (Note n in notes.OrderByDescending(n => n.When))
            {
                sb.Append("<div class=\"ms-note\">" + Ui.Enc(n.Text) + "<small>" + Ui.Enc(Ui.StaffName(staff, n.By)) + " &middot; " + Ui.Time(n.When) + "</small></div>");
            }
            sb.Append("</div>");
            if (canE)
            {
                sb.Append("<div class=\"ms-form\" style=\"margin-top:10px\"><label class=\"ms-field\"><span>Add a note</span><textarea id=\"ms-enote\" rows=\"2\" maxlength=\"500\" placeholder=\"" + Ui.Attr(hint) + "\"></textarea></label><div><button type=\"button\" class=\"ms-btn ms-btn-sm\" onclick=\"AdminMessagesJs.note()\">Save note</button></div></div>");
            }
            return sb.ToString();
        }

        private const int Size = 20;

        private static async Task<string> MainHtml(Ctx c, SiteData site, string arg)
        {
            string st = Ui.MsgStatuses.Any(s => s.Key == arg) ? arg : string.Empty;
            StringBuilder sb = new StringBuilder("<div class=\"ms-ptop\"><div><h1>Messages</h1><p>Questions about dogs and messages from the contact form. Reply by email, then mark them replied or closed.</p></div></div>");
            sb.Append("<div class=\"ms-bar\"><label>Search<input type=\"search\" id=\"ms-q\" list=\"ms-qlist\" placeholder=\"Name, email, dog or words\" maxlength=\"60\" oninput=\"AdminMessagesJs.search()\"></label>" + Ui.DataList("ms-qlist", site.Messages.Select(m => m.Name)));
            sb.Append("<label>Status<select id=\"ms-mst\" onchange=\"AdminMessagesJs.list(1)\"><option value=\"\">Open</option>" + Ui.Opts(Ui.MsgStatuses.Select(s => (s.Key, s.Name)), st) + "<option value=\"all\">Everything</option></select></label>");
            sb.Append("<label>Kind<select id=\"ms-mkind\" onchange=\"AdminMessagesJs.list(1)\"><option value=\"\">Both</option>" + Ui.Opts(Ui.MsgKinds.Select(s => (s.Key, s.Name)), string.Empty) + "</select></label></div>");
            sb.Append("<div class=\"ms-bar\" id=\"ms-perbar\">" + Ui.PeriodBar("ms", "AdminMessagesJs.per", "all", string.Empty, string.Empty) + "</div><div class=\"ms-tbl ms-mstbl\" id=\"ms-list\"></div><div id=\"ms-pager\"></div>");
            return sb.ToString();
        }

        private static string About(SiteData site, Message m)
        {
            Pet? d = Ui.DogOf(site, m.Dog);
            if (d != null)
            {
                return "<span class=\"ms-dmini\">" + (d.Photos.Count > 0 ? "<img src=\"" + Ui.Attr(Ui.Thumb(Ui.Cover(site, d))) + "\" alt=\"\" loading=\"lazy\">" : string.Empty) + "<span><b>" + Ui.Enc(d.Name) + "</b><small>" + Ui.Enc(Ui.NameOf(Ui.DogStatuses, d.Status)) + "</small></span></span>";
            }
            return Ui.Enc(m.Topic != string.Empty ? m.Topic : "General") + "<small>Contact form</small>";
        }

        public async Task<ApiResponse> List()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminMessagesJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return response;
            }
            SiteData site = await Store.LoadSite();
            string term = Ui.Clip(GetDataValue("mq"), 60);
            string st = GetDataValue("mst");
            string kind = GetDataValue("mkind");
            string per = Per(GetDataValue("per"));
            (DateTime? from, DateTime? to) = Ui.Period(Store.Today(site), per, GetDataValue("pfrom"), GetDataValue("pto"));
            IEnumerable<Message> q = site.Messages.Where(m => (st == "all" || (st == string.Empty ? m.Status != "closed" : m.Status == st)) && (kind == string.Empty || m.Kind == kind) && Ui.InPeriod(Store.Local(site, m.Created), from, to));
            if (term != string.Empty)
            {
                q = q.Where(m => (m.Name + " " + m.Email + " " + m.Phone + " " + m.Topic + " " + m.Text + " " + Ui.DogName(site, m.Dog)).Contains(term, StringComparison.OrdinalIgnoreCase));
            }
            List<Message> all = q.OrderByDescending(m => m.Created).ToList();
            int page = Ui.PageOf(GetDataValue("pg"), Size, all.Count);
            StringBuilder sb = new StringBuilder(Head(new[] { ("From", string.Empty), ("About", string.Empty), ("Message", string.Empty), ("Received", string.Empty), ("Status", string.Empty), (string.Empty, string.Empty) }));
            if (all.Count == 0)
            {
                sb.Append("<div class=\"ms-tr\"><span class=\"ms-none ms-wide\">No messages match.</span></div>");
            }
            foreach (Message m in all.Skip((page - 1) * Size).Take(Size))
            {
                sb.Append("<div class=\"ms-tr" + (m.Status == "new" ? " ms-fresh" : string.Empty) + "\"><span class=\"ms-wide\"><b>" + Ui.Enc(m.Name) + "</b><small>" + Ui.Enc(m.Email) + "</small></span><span data-l=\"About\">" + About(site, m) + "</span><span data-l=\"Message\" class=\"ms-wide\"><span class=\"ms-ex\">" + Ui.Enc(m.Text) + "</span></span><span data-l=\"Received\">" + Ui.Time(m.Created) + "</span><span data-l=\"Status\">" + Ui.Pill("ms", Ui.MsgStatuses, m.Status) + "</span><span><button type=\"button\" class=\"ms-btn ms-btn-sm ms-btn-ghost\" onclick=\"AdminMessagesJs.open('" + Ui.Attr(m.Id) + "')\">Open</button></span></div>");
            }
            response.SetElementContents("ms-list", sb.ToString());
            response.SetElementContents("ms-pager", Ui.Pager("ms", page, Size, all.Count, "message", "messages", "AdminMessagesJs.list"));
            response.SetElementContents("ms-perbar", Ui.PeriodBar("ms", "AdminMessagesJs.per", per, GetDataValue("pfrom"), GetDataValue("pto")));
            return response;
        }

        public async Task<ApiResponse> Open()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminMessagesJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return await Refuse(response, c, "message", GetDataValue("mid"), "You don't have access to messages.");
            }
            SiteData site = await Store.LoadSite();
            Message? m = site.Messages.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("mid"), 20));
            if (m == null)
            {
                response.ExecuteScript("AdminMessagesJs.toast('That message no longer exists.');");
                return response;
            }
            Dictionary<string, Staff> staff = Ui.StaffMap(await Store.LoadStaff());
            bool canE = Access.Can(c.U, AreaKey, 'E');
            Pet? d = Ui.DogOf(site, m.Dog);
            string subject = d != null ? "About " + d.Name + " at " + site.Settings.Name : "Your message to " + site.Settings.Name;
            StringBuilder sb = new StringBuilder("<h2>" + Ui.Enc(m.Name) + " <small style=\"font-family:var(--ms-text);font-size:14px;color:var(--ms-ink2)\">" + Ui.Enc(m.Id) + "</small></h2><dl class=\"ms-dl\"><dt>Status</dt><dd>" + Ui.Pill("ms", Ui.MsgStatuses, m.Status) + "</dd><dt>Received</dt><dd>" + Ui.Time(m.Created, "full") + "</dd><dt>About</dt><dd>" + (d != null ? "<a href=\"AdminDogs?id=" + Uri.EscapeDataString(d.Id) + "\">" + Ui.Enc(d.Name) + "</a> &middot; " + Ui.Enc(d.Breed) + " &middot; " + Ui.Pill("ms", Ui.DogStatuses, d.Status) : Ui.Enc(m.Topic != string.Empty ? m.Topic : "General")) + "</dd><dt>Email</dt><dd><a href=\"mailto:" + Ui.Attr(m.Email) + "\">" + Ui.Enc(m.Email) + "</a></dd>" + (m.Phone != string.Empty ? "<dt>Phone</dt><dd><a href=\"tel:" + Ui.Attr(Ui.Digits(m.Phone)) + "\">" + Ui.Enc(m.Phone) + "</a></dd>" : string.Empty) + "</dl><h3>Message</h3><div class=\"ms-lmsg\">" + Ui.Enc(m.Text) + "</div>");
            if (canE)
            {
                sb.Append("<h3>Reply</h3><div class=\"ms-dec\"><a class=\"ms-btn ms-btn-sm ms-btn-accent\" href=\"mailto:" + Ui.Attr(m.Email) + "?subject=" + Uri.EscapeDataString(subject) + "\">Reply by email</a>" + (m.Status != "replied" ? "<button type=\"button\" class=\"ms-btn ms-btn-sm ms-btn-ghost\" onclick=\"AdminMessagesJs.status('replied')\">Mark replied</button>" : string.Empty) + (m.Status != "closed" ? "<button type=\"button\" class=\"ms-btn ms-btn-sm ms-btn-ghost\" onclick=\"AdminMessagesJs.status('closed')\">Mark closed</button>" : "<button type=\"button\" class=\"ms-btn ms-btn-sm ms-btn-ghost\" onclick=\"AdminMessagesJs.status('new')\">Reopen</button>") + "</div>");
                if (d != null && Ui.Adoptable(d))
                {
                    sb.Append("<p class=\"ms-hint\" style=\"margin-top:8px\">Tip: invite them to apply for " + Ui.Enc(d.Name) + " at <a href=\"Adopt?dog=" + Uri.EscapeDataString(d.Id) + "\" target=\"_blank\" rel=\"noopener\">the adoption form</a>.</p>");
                }
            }
            sb.Append(NotesHtml(m.Notes, staff, canE, "Replied by email. Sent the car-ride video."));
            sb.Append("<div class=\"ms-mfoot\">" + (canE ? "<button type=\"button\" class=\"ms-btn ms-btn-danger ms-left\" onclick=\"AdminMessagesJs.remove('" + Ui.Js(m.Name) + "')\">Delete</button>" : string.Empty) + "<button type=\"button\" class=\"ms-btn ms-btn-ghost\" onclick=\"AdminMessagesJs.closeModal()\">Close</button></div>");
            response.SetElementContents("ms-mbody", sb.ToString());
            response.ExecuteScript("AdminMessagesJs.openModal();");
            return response;
        }

        public async Task<ApiResponse> SetStatus()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminMessagesJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "message status", GetDataValue("mid"), "You can look but not change messages.");
            }
            string to = Ui.MsgStatuses.Any(s => s.Key == GetDataValue("mst2")) ? GetDataValue("mst2") : string.Empty;
            string name = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Message? m = site.Messages.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("mid"), 20));
                if (m == null || to == string.Empty)
                {
                    return response;
                }
                m.Status = to;
                if (to == "replied")
                {
                    m.Notes.Add(new Note { When = DateTime.UtcNow, By = c.U.Id, Text = "Replied." });
                }
                name = m.Name;
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "message " + to, name, true);
            response.ExecuteScript("AdminMessagesJs.changed('" + (to == "new" ? "Reopened." : "Marked " + Ui.Js(Ui.NameOf(Ui.MsgStatuses, to).ToLowerInvariant()) + ".") + "');");
            return response;
        }

        public async Task<ApiResponse> AddNote()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminMessagesJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "message note", GetDataValue("mid"), "You can look but not change messages.");
            }
            string text = Regex.Replace(Ui.Clip(GetDataValue("enote"), 500), "\\s+", " ").Trim();
            if (text.Length < 2)
            {
                response.ExecuteScript("AdminMessagesJs.toast('Write the note first.');");
                return response;
            }
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Message? m = site.Messages.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("mid"), 20));
                if (m == null)
                {
                    return response;
                }
                m.Notes.Add(new Note { When = DateTime.UtcNow, By = c.U.Id, Text = text });
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "message note", GetDataValue("mid"), true);
            response.ExecuteScript("AdminMessagesJs.changed('Note saved.');");
            return response;
        }

        public async Task<ApiResponse> Delete()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminMessagesJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "delete message", GetDataValue("mid"), "You can look but not change messages.");
            }
            string name = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Message? m = site.Messages.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("mid"), 20));
                if (m == null)
                {
                    return response;
                }
                name = m.Name;
                site.Messages.Remove(m);
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "delete message", name, true);
            response.ExecuteScript("AdminMessagesJs.deleted();");
            return response;
        }
    }
}
