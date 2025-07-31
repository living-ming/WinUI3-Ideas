using IDEAs.Models;
using IDEAs.Models.Note_Models;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace IDEAs.Services
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Schedule))]
    [JsonSerializable(typeof(Schedule_Event))]
    [JsonSerializable(typeof(ObservableCollection<Schedule_Event>))]
    [JsonSerializable(typeof(ObservableCollection<string>))]

    [JsonSerializable(typeof(Note))]
    [JsonSerializable(typeof(Folder))]

    [JsonSerializable(typeof(ObservableCollection<Item>))]
    [JsonSerializable(typeof(ObservableCollection<NoteImage>))]
    [JsonSerializable(typeof(ObservableCollection<Annotation>))]
    public partial class AppJsonContext : JsonSerializerContext
    {
    }
}

