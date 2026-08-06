using Celeste.Mod.QuickTools.Storage;

namespace Celeste.Mod.QuickTools.UI;

internal sealed record BrowserProfile(
    EntityStoreProfile Store,
    string RootLabel,
    string IdPrefix,
    string WindowId,
    string DragPayloadType,
    string DeleteModalId,
    string ConflictModalId,
    string ListChildId,
    string InlineEditChildId,
    string InlineEditFieldId,
    string EmptyContextMenuId
) {
    public EntityPath Path { get; } = new(Store);

    public static BrowserProfile Quicksave { get; } = new(
        Store: EntityStoreProfile.Quicksave,
        RootLabel: "Quicksaves",
        IdPrefix: "",
        WindowId: "Quicksave Browser",
        DragPayloadType: "QS_FILE",
        DeleteModalId: "Quicksave Confirm Delete",
        ConflictModalId: "Quicksave Conflict",
        ListChildId: "QuicksaveEntryList",
        InlineEditChildId: "QuicksaveInlineEdit",
        InlineEditFieldId: "##inline_edit",
        EmptyContextMenuId: "empty_ctx"
    );

    public static BrowserProfile Ghost { get; } = new(
        Store: EntityStoreProfile.Ghost,
        RootLabel: "Ghosts",
        IdPrefix: "ghost_",
        WindowId: "Ghost Browser",
        DragPayloadType: "GHOST_FILE",
        DeleteModalId: "Ghost Confirm Delete",
        ConflictModalId: "Ghost Conflict",
        ListChildId: "GhostEntryList",
        InlineEditChildId: "GhostInlineEdit",
        InlineEditFieldId: "##ghost_inline_edit",
        EmptyContextMenuId: "ghost_empty_ctx"
    );
}
