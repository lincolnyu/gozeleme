using System.Windows;
using System.Windows.Controls;

namespace FileMatcher.Controls
{
    /// <summary>
    ///  This dock panel always comply with the size of its containing control
    ///  and allocates the available space to its children one after another as per their dock type
    /// </summary>
    public class SubmissiveDockPanel : DockPanel
    {
        #region Constructors

        public SubmissiveDockPanel()
        {
        }

        #endregion

        #region Methods

        protected override Size MeasureOverride(Size constraint)
        {
            var parent = Parent as FrameworkElement;
            if (parent == null)
            {
                return base.MeasureOverride(constraint);
            }

            var width = parent.ActualWidth - Margin.Left - Margin.Right;
            var height = parent.ActualHeight - Margin.Top - Margin.Bottom;

            if (width < 0 || height < 0)
            {
                return base.MeasureOverride(constraint);
            }

            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var remainingRect = new Rect(finalSize);
            for (var i = 0; i < InternalChildren.Count; i++)
            {
                var child = InternalChildren[i];
                var isLast = i == InternalChildren.Count - 1;
                if (isLast)
                {
                    child.Measure(remainingRect.Size);  // this has to be called to notify the child of the size
                    child.Arrange(remainingRect);
                    break;
                }
                var dock = (Dock)child.GetValue(DockProperty);
                child.Measure(remainingRect.Size);
                var rect = remainingRect;
                switch (dock)
                {
                    case Dock.Left:
                        rect.Intersect(new Rect(new Point(rect.Left, rect.Top),
                                                new Size(child.DesiredSize.Width, rect.Height)));
                        remainingRect = new Rect(rect.Right, remainingRect.Top, remainingRect.Width - rect.Width,
                                                 remainingRect.Height);
                        break;
                    case Dock.Right:
                        rect.Intersect(new Rect(new Point(remainingRect.Right - child.DesiredSize.Width, remainingRect.Top),
                                                new Size(child.DesiredSize.Width, remainingRect.Height)));
                        remainingRect = new Rect(remainingRect.Left, remainingRect.Top, remainingRect.Width - rect.Width,
                                                 remainingRect.Height);
                        break;
                    case Dock.Bottom:
                        rect.Intersect(new Rect(new Point(remainingRect.Left, remainingRect.Bottom - child.DesiredSize.Height),
                                           new Size(remainingRect.Width, child.DesiredSize.Height)));
                        remainingRect = new Rect(remainingRect.Left, remainingRect.Top, remainingRect.Width,
                                                 remainingRect.Height - rect.Height);
                        break;
                    default:    // top
                        rect.Intersect(new Rect(new Point(remainingRect.Left, remainingRect.Top),
                                                new Size(remainingRect.Width, child.DesiredSize.Height)));
                        remainingRect = new Rect(remainingRect.Left, rect.Bottom, remainingRect.Width,
                                                 remainingRect.Height - rect.Height);
                        break;
                }
                child.Measure(rect.Size);
                child.Arrange(rect);
            }
            return finalSize;
        }

        #endregion
    }
}
