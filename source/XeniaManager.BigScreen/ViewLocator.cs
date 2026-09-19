using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using XeniaManager.BigScreen.ViewModels;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen;

/// <summary>
/// Given a view model, returns the corresponding view if possible.
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
        {
            return null;
        }

        string name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        Type? type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock
        {
            Text = string.Format(LocalizationHelper.GetText("ViewLocator.NotFound"), name)
        };
    }

    public bool Match(object? data) => data is ViewModelBase;
}