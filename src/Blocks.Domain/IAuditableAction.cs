namespace Blocks.Domain;

public interface IAuditableRequest;

public interface IAuditableAction : IAuditableRequest
{
    int CreatedById { get; set; }
    public bool IsAuthenticated => CreatedById != default;
    DateTime CreatedOn { get; }
    public string Action { get; }
    string? Comment { get; }
}

public interface IAuditableAction<TActionType> : IAuditableAction
    where TActionType : Enum
{
    TActionType ActionType { get; }

    string IAuditableAction.Action => ActionType.ToString();
}

public interface IAuditableActionBy<TUserId> : IAuditableRequest
    where TUserId : class, IParsable<TUserId>
{
    TUserId? CreatedById { get; set; }
    public bool IsAuthenticated => CreatedById is not null;
    DateTime CreatedOn { get; }
    public string Action { get; }
    string? Comment { get; }
}

public interface IAuditableActionBy<TUserId, TActionType> : IAuditableActionBy<TUserId>
    where TUserId : class, IParsable<TUserId>
    where TActionType : Enum
{
    TActionType ActionType { get; }

    string IAuditableActionBy<TUserId>.Action => ActionType.ToString();
}
