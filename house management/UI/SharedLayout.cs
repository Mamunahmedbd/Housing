using System.Drawing;
using System.Windows.Forms;

namespace house_management
{
    /// <summary>
    /// Shared layout helpers used by every module view (Houses, Users,
    /// Tenants, Rentals). Keeping the action-button creation AND layout
    /// logic in ONE place prevents the copy-paste drift that previously
    /// caused buttons to overlap (e.g. the House module's Edit button was
    /// never repositioned by its LayoutHouseViews method, so it collided
    /// with Add whenever the form was resized).
    /// </summary>
    public partial class Form1
    {
        /// <summary>
        /// Right padding (in pixels) between the last action button and the
        /// right edge of the main content panel.
        /// </summary>
        private const int ActionBarRightPadding = 25;

        /// <summary>
        /// Horizontal gap (in pixels) between two adjacent action buttons.
        /// </summary>
        private const int ActionBarGap = 12;

        /// <summary>
        /// Standard width for a single action button. Keeping every module
        /// on the same width makes the cascade arithmetic predictable and
        /// prevents the toolbar from overflowing on narrow windows.
        /// </summary>
        private const int ActionButtonWidth = 110;

        /// <summary>
        /// Factory for module action buttons (Delete / Edit / Add / Reset…).
        /// Every module uses this helper so button height, font, colours,
        /// hover behaviour and corner rounding are visually identical across
        /// Houses, Users, Tenants and Rentals. The initial x position is
        /// only a hint — <see cref="LayoutActionBarRow"/> re-cascades every
        /// button from the right edge on show and on resize.
        /// </summary>
        private Button CreateActionButton(string text, Color backColor, int x, int width)
        {
            Button btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = backColor,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(width, 40),
                Location = new Point(x, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand,
                Visible = false
            };
            btn.FlatAppearance.BorderSize = 0;

            Color original = backColor;
            Color hover = ControlPaint.Light(backColor, 0.15f);
            btn.MouseEnter += (s, e) => btn.BackColor = hover;
            btn.MouseLeave += (s, e) => btn.BackColor = original;

            try { btn.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btn.Width, btn.Height, 10, 10)); } catch { }
            return btn;
        }

        /// <summary>
        /// Cascades action buttons from the right edge of
        /// <see cref="pnlMainContent"/> towards the left, with a uniform
        /// gap. Buttons are supplied in LEFT-TO-RIGHT display order; the
        /// LAST button in the array ends up anchored to the right edge.
        /// Null entries are silently skipped so callers don't need to
        /// guard for unbuilt buttons.
        /// </summary>
        /// <example>
        /// <code>
        /// LayoutActionBarRow(btnDeleteHouse, btnEditHouse, btnAddHouse);
        /// </code>
        /// lays them out visually as:  [Delete] [Edit] [Add]   →|right edge
        /// </example>
        private void LayoutActionBarRow(params Button[] buttons)
        {
            if (pnlMainContent == null || buttons == null) return;

            int rightEdge = pnlMainContent.Width - ActionBarRightPadding;

            // Walk in reverse so the last button is placed at the right edge
            // and every preceding button stacks to its left.
            for (int i = buttons.Length - 1; i >= 0; i--)
            {
                Button btn = buttons[i];
                if (btn == null) continue;

                btn.Left = rightEdge - btn.Width;
                rightEdge = btn.Left - ActionBarGap;
            }
        }
    }
}
