using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using GH_IO.Serialization;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GHAspireConnector;

public abstract class ReadableParamsComponentBase : GH_Component
{
    protected ReadableParamsComponentBase(string name, string nickname, string description, string category, string subCategory)
        : base(name, nickname, description, category, subCategory)
    {
        SyncParameterNicknames();
    }

    public override void AddedToDocument(GH_Document document)
    {
        base.AddedToDocument(document);
        SyncParameterNicknames();
    }

    public override bool Read(GH_IReader reader)
    {
        var result = base.Read(reader);
        SyncParameterNicknames();
        return result;
    }

    protected void SyncParameterNicknames()
    {
        foreach (var param in Params.Input)
        {
            param.NickName = param.Name;
            param.MutableNickName = false;
        }

        foreach (var param in Params.Output)
        {
            param.NickName = param.Name;
            param.MutableNickName = false;
        }
    }

    protected void EnsureConnectedValueList(
        GH_Document document,
        int inputIndex,
        string listName,
        string listDescription,
        IReadOnlyList<(string Name, string Expression)> items,
        int xOffset = 140,
        int yOffset = 10)
    {
        if (Params.Input.Count <= inputIndex)
        {
            return;
        }

        var input = Params.Input[inputIndex];
        if (input.SourceCount > 0 || input.Attributes is null)
        {
            return;
        }

        var valueList = new GH_ValueList
        {
            Name = listName,
            NickName = listName,
            Description = listDescription,
            ListMode = GH_ValueListMode.DropDown
        };

        valueList.CreateAttributes();
        valueList.ListItems.Clear();
        foreach (var item in items)
        {
            valueList.ListItems.Add(new GH_ValueListItem(item.Name, item.Expression));
        }

        var pivot = input.Attributes.Pivot;
        valueList.Attributes.Pivot = new PointF(pivot.X - xOffset, pivot.Y - yOffset);

        document.AddObject(valueList, false);
        input.AddSource(valueList);
        valueList.ExpireSolution(true);
    }

    protected void EnsureConnectedTextValueList(
        GH_Document document,
        int inputIndex,
        string listName,
        string listDescription,
        IReadOnlyList<string> values,
        int xOffset = 140,
        int yOffset = 10)
    {
        EnsureConnectedValueList(
            document,
            inputIndex,
            listName,
            listDescription,
            values.Select(value => (value, QuoteTextExpression(value))).ToList(),
            xOffset,
            yOffset);
    }

    protected void SyncConnectedValueListItems(
        int inputIndex,
        string listName,
        string listDescription,
        IReadOnlyList<(string Name, string Expression)> items)
    {
        var valueList = GetConnectedValueList(inputIndex);
        if (valueList is null)
        {
            return;
        }

        valueList.Name = listName;
        valueList.NickName = listName;
        valueList.Description = listDescription;
        valueList.ListMode = GH_ValueListMode.DropDown;

        var changed = valueList.ListItems.Count != items.Count;
        if (!changed)
        {
            for (var index = 0; index < items.Count; index++)
            {
                var existing = valueList.ListItems[index];
                var expected = items[index];
                if (existing.Name != expected.Name || existing.Expression != expected.Expression)
                {
                    changed = true;
                    break;
                }
            }
        }

        if (!changed)
        {
            return;
        }

        valueList.ListItems.Clear();
        foreach (var item in items)
        {
            valueList.ListItems.Add(new GH_ValueListItem(item.Name, item.Expression));
        }
    }

    protected void SyncConnectedTextValueListItems(
        int inputIndex,
        string listName,
        string listDescription,
        IReadOnlyList<string> values)
    {
        SyncConnectedValueListItems(
            inputIndex,
            listName,
            listDescription,
            values.Select(value => (value, QuoteTextExpression(value))).ToList());
    }

    protected GH_ValueList? GetConnectedValueList(int inputIndex)
    {
        if (Params.Input.Count <= inputIndex)
        {
            return null;
        }

        return Params.Input[inputIndex].Sources.OfType<GH_ValueList>().FirstOrDefault();
    }

    protected static string QuoteTextExpression(string value)
    {
        var escaped = value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");

        return $"\"{escaped}\"";
    }
}