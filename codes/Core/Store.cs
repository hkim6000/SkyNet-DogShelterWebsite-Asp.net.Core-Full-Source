using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using DogShelter.Models;

namespace DogShelter.Core
{
    public static class Store
    {
        private static readonly JsonSerializerOptions ReadOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        private static readonly JsonSerializerOptions WriteOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
        private static readonly SemaphoreSlim LogGate = new SemaphoreSlim(1, 1);
        private static readonly SemaphoreSlim SubGate = new SemaphoreSlim(1, 1);
        private static readonly ConcurrentDictionary<string, TimeZoneInfo> Zones = new ConcurrentDictionary<string, TimeZoneInfo>();

        public static string Root => Directory.GetCurrentDirectory();

        public static string DataDir => Path.Combine(Root, "data");

        public const string SampleUser = "admin";

        public const string SamplePassword = "Walnut2026";

        public const string SampleStaffPassword = "Staff2026";

        public static string CleanId(string? value)
        {
            string v = (value ?? string.Empty).Trim().ToLowerInvariant();
            return Regex.IsMatch(v, "^[a-f0-9]{32}$") ? v : string.Empty;
        }

        private static string FileOf(string name)
        {
            return Path.Combine(DataDir, name + ".json");
        }

        public static async Task<IDisposable> Hold()
        {
            await Gate.WaitAsync();
            return new Release(Gate);
        }

        private sealed class Release : IDisposable
        {
            private SemaphoreSlim? gate;

            public Release(SemaphoreSlim g)
            {
                gate = g;
            }

            public void Dispose()
            {
                gate?.Release();
                gate = null;
            }
        }

        private static async Task<T> Read<T>(string name) where T : new()
        {
            string file = FileOf(name);
            if (!File.Exists(file))
            {
                return new T();
            }
            try
            {
                return JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(file), ReadOpts) ?? new T();
            }
            catch (JsonException)
            {
                return new T();
            }
        }

        private static async Task Write<T>(string name, T data)
        {
            string file = FileOf(name);
            Directory.CreateDirectory(Path.GetDirectoryName(file) ?? Root);
            string tmp = file + "." + Guid.NewGuid().ToString("N").Substring(0, 8) + ".tmp";
            await File.WriteAllTextAsync(tmp, JsonSerializer.Serialize(data, WriteOpts));
            File.Move(tmp, file, true);
        }

        public static async Task<SiteData> LoadSite()
        {
            SiteData site = await Read<SiteData>("site");
            if (site.Anchor != default)
            {
                using (await Hold())
                {
                    site = await Read<SiteData>("site");
                    if (site.Anchor != default)
                    {
                        Rebase(site);
                        await Write("site", site);
                    }
                }
            }
            Fill(site);
            return site;
        }

        public static Task SaveSite(SiteData site)
        {
            return Write("site", site);
        }

        private static void Fill(SiteData site)
        {
            for (int d = 0; d < 7; d++)
            {
                if (!site.Settings.Hours.Any(h => h.Day == d))
                {
                    site.Settings.Hours.Add(new OpenDay { Day = d, Open = d is >= 1 and <= 6 });
                }
            }
            site.Settings.Hours = site.Settings.Hours.OrderBy(h => h.Day).ToList();
            Blocks.Ensure(site);
        }

        private static void Rebase(SiteData site)
        {
            TimeSpan d = TimeSpan.FromDays(Math.Floor((Today(site) - site.Anchor.Date).TotalDays));
            foreach (Pet x in site.Dogs)
            {
                x.Born = x.Born.Add(d);
                x.Intake = x.Intake.Add(d);
                x.Listed = x.Listed?.Add(d);
                x.Adopted = x.Adopted?.Add(d);
                foreach (HealthEvent h in x.Health)
                {
                    h.When = h.When.Add(d);
                }
                foreach (DogEvent h in x.History)
                {
                    h.When = h.When.Add(d);
                }
            }
            foreach (Application x in site.Applications)
            {
                x.Created = x.Created.Add(d);
                x.Visit = x.Visit?.Add(d);
                Shift(x.Notes, d);
            }
            foreach (FosterHome x in site.Fosters)
            {
                x.Created = x.Created.Add(d);
                Shift(x.Notes, d);
            }
            foreach (Placement x in site.Placements)
            {
                x.Start = x.Start.Add(d);
                x.Until = x.Until?.Add(d);
                x.Ended = x.Ended?.Add(d);
            }
            foreach (VolunteerInfo x in site.Volunteers)
            {
                x.Created = x.Created.Add(d);
                foreach (HourLog h in x.Hours)
                {
                    h.When = h.When.Add(d);
                }
                Shift(x.Notes, d);
            }
            foreach (Event x in site.Events)
            {
                x.Date = x.Date.Add(d);
                foreach (Rsvp r in x.Rsvps)
                {
                    r.Created = r.Created.Add(d);
                }
            }
            foreach (Donation x in site.Donations)
            {
                x.Created = x.Created.Add(d);
                x.Received = x.Received?.Add(d);
                Shift(x.Notes, d);
            }
            foreach (Message x in site.Messages)
            {
                x.Created = x.Created.Add(d);
                Shift(x.Notes, d);
            }
            foreach (MediaItem x in site.Media)
            {
                x.When = x.When.Add(d);
            }
            foreach (PageDoc x in site.Pages.Values)
            {
                x.Saved = x.Saved.Add(d);
                foreach (PageVersion v in x.Versions)
                {
                    v.When = v.When.Add(d);
                }
            }
            site.Anchor = default;
        }

        private static void Shift(List<Note> notes, TimeSpan d)
        {
            foreach (Note n in notes)
            {
                n.When = n.When.Add(d);
            }
        }

        public static Task<List<Staff>> LoadStaff()
        {
            return Read<List<Staff>>("staff");
        }

        public static Task SaveStaff(List<Staff> staff)
        {
            return Write("staff", staff);
        }

        public static Task<List<Activity>> LoadActivity()
        {
            return Read<List<Activity>>("activity");
        }

        public static async Task Log(string user, string action, string target, bool ok)
        {
            await LogGate.WaitAsync();
            try
            {
                List<Activity> list = await Read<List<Activity>>("activity");
                list.Insert(0, new Activity { When = DateTime.UtcNow, User = user, Action = action, Target = target.Length > 120 ? target.Substring(0, 120) : target, Ok = ok });
                if (list.Count > 5000)
                {
                    list.RemoveRange(5000, list.Count - 5000);
                }
                await Write("activity", list);
            }
            finally
            {
                LogGate.Release();
            }
        }

        public static async Task<bool> AddSubscriber(string email)
        {
            await SubGate.WaitAsync();
            try
            {
                List<Subscriber> list = await Read<List<Subscriber>>("subscribers");
                if (list.Any(s => s.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
                list.Add(new Subscriber { Email = email, When = DateTime.UtcNow });
                await Write("subscribers", list);
                return true;
            }
            finally
            {
                SubGate.Release();
            }
        }

        public static async Task EnsureAdmin()
        {
            if (File.Exists(FileOf("staff")))
            {
                return;
            }
            using (await Hold())
            {
                if (File.Exists(FileOf("staff")))
                {
                    return;
                }
                List<Staff> staff = new List<Staff> { new Staff { Id = "u-admin", Login = SampleUser, Name = "Shelter Manager", Title = "Shelter manager", Admin = true, Active = true, Hash = Access.HashPassword(SamplePassword) } };
                await SaveStaff(staff);
            }
        }

        public static TimeZoneInfo Zone(string id)
        {
            return Zones.GetOrAdd(id ?? string.Empty, k =>
            {
                try
                {
                    return k == string.Empty ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(k);
                }
                catch (TimeZoneNotFoundException)
                {
                    return TimeZoneInfo.Utc;
                }
                catch (InvalidTimeZoneException)
                {
                    return TimeZoneInfo.Utc;
                }
            });
        }

        public static bool KnownZone(string id)
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(id);
                return true;
            }
            catch (TimeZoneNotFoundException)
            {
                return false;
            }
            catch (InvalidTimeZoneException)
            {
                return false;
            }
        }

        public static DateTime Now(SiteData s)
        {
            return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone(s.Settings.TimeZone)), DateTimeKind.Unspecified);
        }

        public static DateTime Today(SiteData s)
        {
            return Now(s).Date;
        }

        public static DateTime Local(SiteData s, DateTime utc)
        {
            return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone(s.Settings.TimeZone)), DateTimeKind.Unspecified);
        }

        public static TimeSpan? Hm(string? v)
        {
            return TimeSpan.TryParseExact((v ?? string.Empty).Trim(), "hh\\:mm", CultureInfo.InvariantCulture, out TimeSpan t) && t >= TimeSpan.Zero && t <= TimeSpan.FromHours(24) ? t : null;
        }

        public static string NextId(IEnumerable<string> ids, string prefix, int start)
        {
            int max = start;
            foreach (string id in ids)
            {
                if (id.StartsWith(prefix, StringComparison.Ordinal) && int.TryParse(id.Substring(prefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n > max)
                {
                    max = n;
                }
            }
            return prefix + (max + 1).ToString(CultureInfo.InvariantCulture);
        }
    }
}
