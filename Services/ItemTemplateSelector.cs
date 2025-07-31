using IDEAs.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace IDEAs.Services
{

    public class ItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate FolderTemplate { get; set; }
        public DataTemplate NoteTemplate { get; set; }
        public DataTemplate ScheduleTemplate { get; set; }
        public DataTemplate CalendarTemplate { get; set; }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ItemTemplateSelector))]
        public ItemTemplateSelector()
        {
            // 防止属性被裁
            _ = FolderTemplate;
            _ = NoteTemplate;
            _ = ScheduleTemplate;
            _ = CalendarTemplate;
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item == null)
                return null;

            try
            {
                switch (item)
                {
                    case Folder:
                        return FolderTemplate;
                    case Note:
                        return NoteTemplate;
                    case Schedule:
                        return ScheduleTemplate;
                    case Calendar:
                        return CalendarTemplate;
                    default:
                        return base.SelectTemplateCore(item, container);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TemplateSelector Error: {ex.Message}");
                return base.SelectTemplateCore(item, container);
            }
        }
    }
}
