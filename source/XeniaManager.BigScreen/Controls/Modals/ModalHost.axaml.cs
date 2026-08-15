using System.Collections.Generic;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.ViewModels.Modals;

namespace XeniaManager.BigScreen.Controls.Modals;

/// <summary>
/// Hosts the modal stack: every open modal renders as a layered full-window
/// entry (bottom to top), each resolved through the app's data templates.
/// The top entry is the only one that receives input.
/// </summary>
public partial class ModalHost : UserControl
{
    private readonly IModalService _modalService;

    public ModalHost()
    {
        InitializeComponent();

        _modalService = App.Services.GetRequiredService<IModalService>();
        _modalService.StackChanged += OnStackChanged;
    }

    /// <summary>
    /// Rebuilds the layered stack - later modals overlay earlier ones.
    /// </summary>
    private void OnStackChanged()
    {
        StackHost.Children.Clear();
        IReadOnlyList<ModalViewModelBase> stack = _modalService.Stack;
        for (int i = 0; i < stack.Count; i++)
        {
            StackHost.Children.Add(new ContentControl
            {
                Content = stack[i],
                IsHitTestVisible = i == stack.Count - 1
            });
        }
    }
}
