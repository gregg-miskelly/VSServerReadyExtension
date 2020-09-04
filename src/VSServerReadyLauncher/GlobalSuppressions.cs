// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "VSSDK004:Use BackgroundLoad flag in ProvideAutoLoad attribute for asynchronous auto load.", Justification = "Auto load is required for this package to work", Scope = "type", Target = "~T:VSServerReadyLauncher.VSServerReadyLauncherPackage")]
