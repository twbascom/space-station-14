using Content.Shared.Actions;

namespace Content.Shared.Silicons.StationAi;

public sealed partial class MalfExplodeApcActionEvent : EntityTargetActionEvent;

public sealed partial class MalfOverrideVentActionEvent : EntityTargetActionEvent;

public sealed partial class MalfSiphonVentActionEvent : EntityTargetActionEvent;

public sealed partial class MalfImmovableRodActionEvent : WorldTargetActionEvent;

public sealed partial class MalfRollCoreActionEvent : WorldTargetActionEvent;

public sealed partial class MalfShockAirlockActionEvent : EntityTargetActionEvent;

public sealed partial class MalfBlackoutActionEvent : InstantActionEvent;
