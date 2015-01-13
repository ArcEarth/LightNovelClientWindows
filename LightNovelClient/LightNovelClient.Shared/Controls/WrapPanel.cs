using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace LightNovel.Controls
{
    public class WrapPanel : Panel
    {
        #region Orientation Property
        //Gets or sets whether elements are stacked vertically or horizontally.
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientation), typeof(WrapPanel), new PropertyMetadata(Orientation.Vertical, OnOrientationPropertyChanged));

        private static void OnOrientationPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            (source as WrapPanel).InvalidateMeasure();
        }
        #endregion


        #region double BlockSize Property
        //Gets or sets the fixed dimension size of block.
        //Vertical orientation => BlockWidth
        //Horizontal orientation => BlockHeight
        public double BlockSize
        {
            get { return (double)GetValue(BlockSizeProperty); }
            set { SetValue(BlockSizeProperty, value); }
        }

        public static readonly DependencyProperty BlockSizeProperty =
            DependencyProperty.Register("BlockSize", typeof(double), typeof(WrapPanel), new PropertyMetadata(100.0, OnBlockSizePropertyChanged));

        private static void OnBlockSizePropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            (source as WrapPanel).InvalidateMeasure();
        }
        #endregion


        #region double BlockSpacing Property
        //Gets or sets the amount of space in pixels between blocks.
        public double BlockSpacing
        {
            get { return (double)GetValue(BlockSpacingProperty); }
            set { SetValue(BlockSpacingProperty, value); }
        }

        public static readonly DependencyProperty BlockSpacingProperty =
            DependencyProperty.Register("BlockSpacing", typeof(double), typeof(WrapPanel), new PropertyMetadata(0.0, OnBlockSpacingPropertyChanged));

        private static void OnBlockSpacingPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            (source as WrapPanel).InvalidateMeasure();
        }
        #endregion


        public WrapPanel()
            : base()
        {

        }

        protected override Size MeasureOverride(Size availableSize)
        {
            switch (Orientation)
            {
                case Orientation.Horizontal:
                    return MeasureOverrideHorizontal(availableSize);

                case Orientation.Vertical:
                default:
                    return MeasureOverrideVertical(availableSize);
            }
        }

        private Size MeasureOverrideVertical(Size availableSize)
        {
            //Create available size for child control
            //In Vertical orientation child control can have a maximum width of BlockSize
            //And it's height can be "unlimited" - I want to know what height would control like to have at given width
            Size childAvailableSize = new Size(BlockSize, double.PositiveInfinity);

            //Next, i want to stack my child controls under each other (i call it block), until i reach my available height. 
            //From that point i want to begin another block of controls next to the current one.

            int blockCount = 0;
            if (Children.Count > 0) //If i have any child controls, than i will have at least one block.
                blockCount = 1;

            var remainingSpace = availableSize.Height; //Set my limit as my available height.
            foreach (var item in Children)
            {
                item.Measure(childAvailableSize); //Let the child measure itself (result of this will be in item.DesiredSize
                if (item.DesiredSize.Height > remainingSpace) //If there is not enough space for this control
                {
                    //Then we will start a new block, but only if the current block is not empty
                    //if its empty, then remaining space will be equal to available height.
                    if (remainingSpace != availableSize.Height)
                    {
                        remainingSpace = availableSize.Height;
                        blockCount++;   //Reset remaining space and increase block count.
                    }
                }
                //In any case, decrease remaining space by desired height of control.
                remainingSpace -= item.DesiredSize.Height;
            }

            //Now we need to report back how much size we want,
            //thats number of blocks * their width, plus spaces between blocks
            //And for height, we will take what ever we can get.
            Size desiredSize = new Size();
            if (blockCount > 0)
                desiredSize.Width = (blockCount * BlockSize) + ((blockCount - 1) * BlockSpacing);
            else desiredSize.Width = 0;
            desiredSize.Height = availableSize.Height;

            return desiredSize;
        }

        private Size MeasureOverrideHorizontal(Size availableSize)
        {
            Size childAvailableSize = new Windows.Foundation.Size(double.PositiveInfinity, BlockSize);

            int blockCount = 0;
            if (Children.Count > 0)
                blockCount = 1;

            var remainingSpace = availableSize.Width;
            foreach (var item in Children)
            {
                item.Measure(childAvailableSize);
                if (item.DesiredSize.Width > remainingSpace)
                {
                    if (remainingSpace != availableSize.Width)
                    {
                        remainingSpace = availableSize.Width;
                        blockCount++;
                    }
                }

                remainingSpace -= item.DesiredSize.Width;
            }

            Size desiredSize = new Size();
            if (blockCount > 0)
                desiredSize.Height = (blockCount * BlockSize) + ((blockCount - 1) * BlockSpacing);
            else desiredSize.Height = 0;
            desiredSize.Width = availableSize.Width;

            return desiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            switch (Orientation)
            {
                case Orientation.Horizontal:
                    return ArrangeOverrideHorizontal(finalSize);
                case Orientation.Vertical:
                default:
                    return ArrangeOverrideVertical(finalSize);
            }
        }

        private Size ArrangeOverrideVertical(Size finalSize)
        {
            //Each child control will be placed in rectangle with width of BlockSize
            //and height of child controls desired height.
            //Upper left corner of first controls rectangle will initialy start at 0,0 relative to this control
            //and move down by height of control and more to the left by BlockSize once the block runs out of free space
            double offsetX = 0;
            double offsetY = 0;
            foreach (var item in Children)
            {
                //If item will fit into remaining space, ....
                if ((finalSize.Height - offsetY) < item.DesiredSize.Height)
                {
                    if (offsetY != 0) //and the current block is not empty. (same rules as in measureoverride)
                    {
                        offsetX += BlockSpacing; //We will increse offset from left by the block size
                        offsetX += BlockSize;    //and spacing between blocks
                        offsetY = 0;             //and finally reset offset from top
                    }
                }
                //Create rectangle for child control
                Rect rect = new Rect(new Point(offsetX, offsetY), new Size(BlockSize, item.DesiredSize.Height));
                //And make it arrange within the rectangle, ...
                item.Arrange(rect);
                //Increment the offset by height.
                offsetY += item.DesiredSize.Height;
            }
            return base.ArrangeOverride(finalSize);
        }

        private Size ArrangeOverrideHorizontal(Size finalSize)
        {
            double offsetX = 0;
            double offsetY = 0;
            foreach (var item in Children)
            {
                if ((finalSize.Width - offsetX) < item.DesiredSize.Width)
                {
                    if (offsetX != 0)
                    {
                        offsetY += BlockSpacing;
                        offsetY += BlockSize;
                        offsetX = 0;
                    }
                }
                Rect rect = new Rect(new Point(offsetX, offsetY), new Size(item.DesiredSize.Width, BlockSize));
                item.Arrange(rect);
                offsetX += item.DesiredSize.Width;
            }
            return base.ArrangeOverride(finalSize);
        }
    }

}
