namespace EZAccess.Data;

public class BeforeCRUDEventArgs<TModel> : EventArgs
{
    public TModel Model { get; set; }

    public BeforeCRUDEventArgs(TModel model)
    {
        Model = model;
    }

    public bool Cancel { get; set; }
}
