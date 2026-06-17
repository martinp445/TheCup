using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TheCup_Application.Models;
using TheCup_Application.ViewModels;
using TheCup_Presentation.DragDrop;

namespace TheCup_Presentation.Views;

public partial class GroupsView
{
    public const string TeamDragFormat = "TheCup.TeamDrag";

    public GroupsView()
    {
        InitializeComponent();
    }

    private GroupsViewModel? ViewModel => DataContext as GroupsViewModel;

    private void Team_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel is null || !ViewModel.CanMoveTeamsBetweenGroups)
        {
            return;
        }

        if (sender is not FrameworkElement element)
        {
            return;
        }

        if (element.DataContext is not TeamSummary team)
        {
            return;
        }

        var sourceGroup = FindParentGroup(element);
        if (sourceGroup is null)
        {
            return;
        }

        var dragInfo = new TeamDragInfo
        {
            TeamId = team.Id,
            SourceGroupId = sourceGroup.Id
        };

        var data = new DataObject(TeamDragFormat, dragInfo);
        System.Windows.DragDrop.DoDragDrop(element, data, DragDropEffects.Move);
    }

    private void GroupBorder_DragOver(object sender, DragEventArgs e)
    {
        if (!CanAcceptDrop(e, sender, out _))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void GroupBorder_DragEnter(object sender, DragEventArgs e)
    {
        if (sender is Border border && CanAcceptDrop(e, sender, out _))
        {
            border.Opacity = 0.85;
        }
    }

    private void GroupBorder_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border border)
        {
            border.Opacity = 1;
        }
    }

    private async void GroupBorder_Drop(object sender, DragEventArgs e)
    {
        if (sender is Border border)
        {
            border.Opacity = 1;
        }

        if (!CanAcceptDrop(e, sender, out var targetGroup) || ViewModel is null)
        {
            return;
        }

        if (e.Data.GetData(TeamDragFormat) is not TeamDragInfo dragInfo)
        {
            return;
        }

        e.Handled = true;

        await ViewModel
            .MoveTeamBetweenGroupsAsync(dragInfo.TeamId, dragInfo.SourceGroupId, targetGroup.Id)
            .ConfigureAwait(true);
    }

    private bool CanAcceptDrop(DragEventArgs e, object sender, out GroupSummary targetGroup)
    {
        targetGroup = null!;

        if (ViewModel is null || !ViewModel.CanMoveTeamsBetweenGroups)
        {
            return false;
        }

        if (!e.Data.GetDataPresent(TeamDragFormat))
        {
            return false;
        }

        if (e.Data.GetData(TeamDragFormat) is not TeamDragInfo dragInfo)
        {
            return false;
        }

        if (sender is not FrameworkElement element || element.DataContext is not GroupSummary group)
        {
            return false;
        }

        if (group.Id == dragInfo.SourceGroupId)
        {
            return false;
        }

        targetGroup = group;
        return true;
    }

    private static GroupSummary? FindParentGroup(DependencyObject child)
    {
        while (child is not null)
        {
            if (child is FrameworkElement element && element.DataContext is GroupSummary group)
            {
                return group;
            }

            child = VisualTreeHelper.GetParent(child);
        }

        return null;
    }
}
