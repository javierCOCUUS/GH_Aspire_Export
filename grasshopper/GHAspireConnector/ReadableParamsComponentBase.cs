using Grasshopper.Kernel;
using GH_IO.Serialization;

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
}