using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using GH_IO.Serialization;
using System.Collections.Generic;
using System.Drawing;

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
}