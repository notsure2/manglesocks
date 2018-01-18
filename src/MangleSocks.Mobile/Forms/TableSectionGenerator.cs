using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Humanizer;
using Xamarin.Forms;

namespace MangleSocks.Mobile.Forms
{
    static class TableSectionGenerator
    {
        public static IEnumerable<Cell> GenerateSettingsFormCells(object instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            foreach (var property in instance.GetType().GetProperties(
                BindingFlags.Public | BindingFlags.Instance))
            {
                var entryCell = new EntryCell();
                if (property.PropertyType == typeof(sbyte)
                    || property.PropertyType == typeof(byte)
                    || property.PropertyType == typeof(short)
                    || property.PropertyType == typeof(ushort)
                    || property.PropertyType == typeof(int)
                    || property.PropertyType == typeof(uint)
                    || property.PropertyType == typeof(long)
                    || property.PropertyType == typeof(ulong)
                    || property.PropertyType == typeof(decimal)
                    || property.PropertyType == typeof(float)
                    || property.PropertyType == typeof(double))
                {
                    entryCell.Keyboard = Keyboard.Numeric;
                }

                entryCell.Label = property.GetCustomAttribute<DisplayAttribute>()?.Name ?? property.Name.Humanize(LetterCasing.Title);
                entryCell.HorizontalTextAlignment = TextAlignment.End;
                entryCell.SetBinding(EntryCell.TextProperty, new Binding { Source = instance, Path = property.Name });
                yield return entryCell;
            }
        }
    }
}