namespace DogShelter.Models
{
    public class SiteData
    {
        public DateTime Anchor { get; set; }
        public Settings Settings { get; set; } = new Settings();
        public List<Pet> Dogs { get; set; } = new List<Pet>();
        public List<Application> Applications { get; set; } = new List<Application>();
        public List<FosterHome> Fosters { get; set; } = new List<FosterHome>();
        public List<Placement> Placements { get; set; } = new List<Placement>();
        public List<VolunteerInfo> Volunteers { get; set; } = new List<VolunteerInfo>();
        public List<Event> Events { get; set; } = new List<Event>();
        public List<Donation> Donations { get; set; } = new List<Donation>();
        public List<Message> Messages { get; set; } = new List<Message>();
        public List<MediaItem> Media { get; set; } = new List<MediaItem>();
        public Dictionary<string, PageDoc> Pages { get; set; } = new Dictionary<string, PageDoc>();
    }

    public class Settings
    {
        public string Name { get; set; } = string.Empty;
        public string Tagline { get; set; } = string.Empty;
        public string Town { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Charity { get; set; } = string.Empty;
        public string Accent { get; set; } = "#9A5B2E";
        public string Theme { get; set; } = "light";
        public decimal FeePuppy { get; set; } = 350m;
        public decimal FeeAdult { get; set; } = 250m;
        public decimal FeeSenior { get; set; } = 100m;
        public string FeeNote { get; set; } = string.Empty;
        public string Amounts { get; set; } = "25,50,100,250";
        public string PledgeText { get; set; } = string.Empty;
        public string WishList { get; set; } = string.Empty;
        public bool Clock24 { get; set; }
        public string TimeZone { get; set; } = "America/New_York";
        public List<OpenDay> Hours { get; set; } = new List<OpenDay>();
    }

    public class OpenDay
    {
        public int Day { get; set; }
        public bool Open { get; set; }
        public string Start { get; set; } = "11:00";
        public string End { get; set; } = "17:00";
    }

    public class Pet
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Breed { get; set; } = string.Empty;
        public string Sex { get; set; } = "female";
        public DateTime Born { get; set; }
        public string Size { get; set; } = "medium";
        public int Weight { get; set; }
        public string Color { get; set; } = string.Empty;
        public string Energy { get; set; } = "medium";
        public string Kids { get; set; } = "ask";
        public string Dogs { get; set; } = "ask";
        public string Cats { get; set; } = "ask";
        public bool HouseTrained { get; set; }
        public bool Vaccinated { get; set; }
        public bool Fixed { get; set; }
        public bool Chipped { get; set; }
        public string Needs { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public string Status { get; set; } = "intake";
        public bool Featured { get; set; }
        public bool Urgent { get; set; }
        public string Bonded { get; set; } = string.Empty;
        public List<string> Photos { get; set; } = new List<string>();
        public string Story { get; set; } = string.Empty;
        public List<string> Traits { get; set; } = new List<string>();
        public DateTime Intake { get; set; }
        public DateTime? Listed { get; set; }
        public DateTime? Adopted { get; set; }
        public string Adopter { get; set; } = string.Empty;
        public string HappyTail { get; set; } = string.Empty;
        public List<HealthEvent> Health { get; set; } = new List<HealthEvent>();
        public List<DogEvent> History { get; set; } = new List<DogEvent>();
        public int Views { get; set; }
        public int Saves { get; set; }
    }

    public class HealthEvent
    {
        public DateTime When { get; set; }
        public string Kind { get; set; } = "vet";
        public string Note { get; set; } = string.Empty;
    }

    public class DogEvent
    {
        public DateTime When { get; set; }
        public string Event { get; set; } = string.Empty;
        public string By { get; set; } = string.Empty;
    }

    public class Application
    {
        public string Id { get; set; } = string.Empty;
        public string Dog { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Home { get; set; } = "house";
        public string Owns { get; set; } = "own";
        public string Yard { get; set; } = "fenced";
        public string Kids { get; set; } = string.Empty;
        public string Pets { get; set; } = string.Empty;
        public string Experience { get; set; } = string.Empty;
        public string Alone { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = "new";
        public string Reviewer { get; set; } = string.Empty;
        public DateTime? Visit { get; set; }
        public DateTime Created { get; set; }
        public List<Note> Notes { get; set; } = new List<Note>();
    }

    public class Note
    {
        public DateTime When { get; set; }
        public string By { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class FosterHome
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Home { get; set; } = "house";
        public string Yard { get; set; } = "fenced";
        public string Pets { get; set; } = string.Empty;
        public string Kids { get; set; } = string.Empty;
        public List<string> Sizes { get; set; } = new List<string>();
        public string Length { get; set; } = "short";
        public bool Medical { get; set; }
        public bool Puppies { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = "new";
        public DateTime Created { get; set; }
        public List<Note> Notes { get; set; } = new List<Note>();
    }

    public class Placement
    {
        public string Id { get; set; } = string.Empty;
        public string Dog { get; set; } = string.Empty;
        public string Home { get; set; } = string.Empty;
        public DateTime Start { get; set; }
        public DateTime? Until { get; set; }
        public DateTime? Ended { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    public class VolunteerInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
        public List<string> Times { get; set; } = new List<string>();
        public bool Adult { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = "new";
        public DateTime Created { get; set; }
        public List<HourLog> Hours { get; set; } = new List<HourLog>();
        public List<Note> Notes { get; set; } = new List<Note>();
    }

    public class HourLog
    {
        public DateTime When { get; set; }
        public decimal Hours { get; set; }
        public string Note { get; set; } = string.Empty;
        public string By { get; set; } = string.Empty;
    }

    public class Event
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Kind { get; set; } = "open";
        public DateTime Date { get; set; }
        public string Start { get; set; } = "11:00";
        public string End { get; set; } = "15:00";
        public string Place { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public bool Published { get; set; } = true;
        public List<Rsvp> Rsvps { get; set; } = new List<Rsvp>();
    }

    public class Rsvp
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int Guests { get; set; } = 1;
        public DateTime Created { get; set; }
        public bool Arrived { get; set; }
    }

    public class Donation
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Every { get; set; } = "once";
        public string Fund { get; set; } = "general";
        public string Dog { get; set; } = string.Empty;
        public string Honor { get; set; } = string.Empty;
        public string Method { get; set; } = "check";
        public bool Anonymous { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = "pledged";
        public DateTime Created { get; set; }
        public DateTime? Received { get; set; }
        public List<Note> Notes { get; set; } = new List<Note>();
    }

    public class Message
    {
        public string Id { get; set; } = string.Empty;
        public string Kind { get; set; } = "contact";
        public string Dog { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Status { get; set; } = "new";
        public DateTime Created { get; set; }
        public List<Note> Notes { get; set; } = new List<Note>();
    }

    public class Staff
    {
        public string Id { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public bool Admin { get; set; }
        public bool Active { get; set; } = true;
        public Dictionary<string, string> Perms { get; set; } = new Dictionary<string, string>();
    }

    public class Activity
    {
        public DateTime When { get; set; }
        public string User { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public bool Ok { get; set; } = true;
    }

    public class Session
    {
        public string Tok { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public DateTime Exp { get; set; }
    }

    public class Subscriber
    {
        public string Email { get; set; } = string.Empty;
        public DateTime When { get; set; }
    }

    public class MediaItem
    {
        public string Id { get; set; } = string.Empty;
        public string Kind { get; set; } = "photo";
        public string Title { get; set; } = string.Empty;
        public string Alt { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        public string Full { get; set; } = string.Empty;
        public string Thumb { get; set; } = string.Empty;
        public int Seconds { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public long Bytes { get; set; }
        public string By { get; set; } = string.Empty;
        public DateTime When { get; set; }
    }

    public class PageDoc
    {
        public List<Block> Blocks { get; set; } = new List<Block>();
        public List<PageVersion> Versions { get; set; } = new List<PageVersion>();
        public DateTime Saved { get; set; }
        public string By { get; set; } = string.Empty;
    }

    public class PageVersion
    {
        public DateTime When { get; set; }
        public string By { get; set; } = string.Empty;
        public List<Block> Blocks { get; set; } = new List<Block>();
    }

    public class Block
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = "text";
        public string Source { get; set; } = string.Empty;
        public bool Hidden { get; set; }
        public string Kicker { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Media { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string Label2 { get; set; } = string.Empty;
        public string Link2 { get; set; } = string.Empty;
        public bool Flip { get; set; }
        public string Extra { get; set; } = string.Empty;
        public List<BlockItem> Items { get; set; } = new List<BlockItem>();
    }

    public class BlockItem
    {
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }
}
