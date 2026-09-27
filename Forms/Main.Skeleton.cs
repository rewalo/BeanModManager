using BeanModManager.Controls;
using System.Collections.Generic;
using System.Windows.Forms;

namespace BeanModManager
{
    public partial class Main : Form
    {
        private List<SkeletonModCard> _skeletonInstalledCards = new List<SkeletonModCard>();
        private List<SkeletonModCard> _skeletonStoreCards = new List<SkeletonModCard>();

        private void ShowSkeletonLoaders(int installedCount, int storeCount)
        {
            if (panelInstalled == null || panelStore == null) return;

            HideSkeletonLoaders();

            panelInstalled.SuspendLayout();
            panelStore.SuspendLayout();
            panelInstalled.SuppressScrollbarInvalidation(true);
            panelStore.SuppressScrollbarInvalidation(true);

            try
            {
                panelInstalled.Controls.Clear();
                panelStore.Controls.Clear();

                for (int i = 0; i < installedCount; i++)
                {
                    var skeleton = new SkeletonModCard();
                    skeleton.Visible = false;
                    _skeletonInstalledCards.Add(skeleton);
                    panelInstalled.Controls.Add(skeleton);
                }

                for (int i = 0; i < storeCount; i++)
                {
                    var skeleton = new SkeletonModCard();
                    skeleton.Visible = false;
                    _skeletonStoreCards.Add(skeleton);
                    panelStore.Controls.Add(skeleton);
                }
            }
            finally
            {
                panelInstalled.SuppressScrollbarInvalidation(false);
                panelStore.SuppressScrollbarInvalidation(false);

                panelInstalled.ResumeLayout(true);
                panelStore.ResumeLayout(true);

                foreach (var skeleton in _skeletonInstalledCards)
                {
                    if (skeleton != null && !skeleton.IsDisposed)
                    {
                        skeleton.Visible = true;
                    }
                }

                foreach (var skeleton in _skeletonStoreCards)
                {
                    if (skeleton != null && !skeleton.IsDisposed)
                    {
                        skeleton.Visible = true;
                    }
                }
            }
        }

        private void HideSkeletonLoaders()
        {
            foreach (var skeleton in _skeletonInstalledCards)
            {
                skeleton?.Dispose();
            }
            _skeletonInstalledCards.Clear();

            foreach (var skeleton in _skeletonStoreCards)
            {
                skeleton?.Dispose();
            }
            _skeletonStoreCards.Clear();
        }

        private void ShowStoreSkeletonLoaders(int storeCount)
        {
            if (panelStore == null) return;

            foreach (var skeleton in _skeletonStoreCards)
            {
                skeleton?.Dispose();
            }
            _skeletonStoreCards.Clear();

            panelStore.SuspendLayout();
            panelStore.SuppressScrollbarInvalidation(true);
            try
            {
                panelStore.Controls.Clear();

                for (int i = 0; i < storeCount; i++)
                {
                    var skeleton = new SkeletonModCard { Visible = false };
                    _skeletonStoreCards.Add(skeleton);
                    panelStore.Controls.Add(skeleton);
                }
            }
            finally
            {
                panelStore.SuppressScrollbarInvalidation(false);
                panelStore.ResumeLayout(true);

                foreach (var skeleton in _skeletonStoreCards)
                {
                    if (skeleton != null && !skeleton.IsDisposed)
                        skeleton.Visible = true;
                }
            }
        }

        private void HideStoreSkeletonLoaders()
        {
            foreach (var skeleton in _skeletonStoreCards)
            {
                skeleton?.Dispose();
            }
            _skeletonStoreCards.Clear();
        }

        private void ReplaceSkeletonsWithCards(List<ModCard> installedCards, List<ModCard> storeCards)
        {
            if (panelInstalled == null || panelStore == null) return;

            var skeletonsToDispose = new List<SkeletonModCard>();
            skeletonsToDispose.AddRange(_skeletonInstalledCards);
            _skeletonInstalledCards.Clear();
            if (!_suppressStorePanelUpdates)
            {
                skeletonsToDispose.AddRange(_skeletonStoreCards);
                _skeletonStoreCards.Clear();
            }

            panelInstalled.SuspendLayout();
            panelInstalled.SuppressScrollbarInvalidation(true);
            if (!_suppressStorePanelUpdates)
            {
                panelStore.SuspendLayout();
                panelStore.SuppressScrollbarInvalidation(true);
            }

            try
            {
                foreach (var card in installedCards)
                {
                    if (panelInstalled.Controls.Contains(card))
                        panelInstalled.Controls.Remove(card);
                }

                if (!_suppressStorePanelUpdates)
                {
                    foreach (var card in installedCards)
                    {
                        if (panelStore.Controls.Contains(card))
                            panelStore.Controls.Remove(card);
                    }

                    foreach (var card in storeCards)
                    {
                        if (panelInstalled.Controls.Contains(card))
                            panelInstalled.Controls.Remove(card);
                        if (panelStore.Controls.Contains(card))
                            panelStore.Controls.Remove(card);
                    }
                }

                panelInstalled.Controls.Clear();
                if (!_suppressStorePanelUpdates)
                {
                    panelStore.Controls.Clear();
                }

                foreach (var card in installedCards)
                {
                    card.Visible = false;
                    panelInstalled.Controls.Add(card);
                }
                for (int i = installedCards.Count - 1; i >= 0; i--)
                {
                    panelInstalled.Controls.SetChildIndex(installedCards[i], i);
                }

                if (!_suppressStorePanelUpdates)
                {
                    foreach (var card in storeCards)
                    {
                        card.Visible = false;
                        panelStore.Controls.Add(card);
                    }
                    for (int i = storeCards.Count - 1; i >= 0; i--)
                    {
                        panelStore.Controls.SetChildIndex(storeCards[i], i);
                    }
                }
            }
            finally
            {
                panelInstalled.SuppressScrollbarInvalidation(false);
                if (!_suppressStorePanelUpdates)
                {
                    panelStore.SuppressScrollbarInvalidation(false);
                }

                panelInstalled.ResumeLayout(true);
                if (!_suppressStorePanelUpdates)
                {
                    panelStore.ResumeLayout(true);
                }

                foreach (var card in installedCards)
                {
                    if (card != null && !card.IsDisposed && panelInstalled.Controls.Contains(card))
                    {
                        card.Visible = true;
                    }
                }

                if (!_suppressStorePanelUpdates)
                {
                    foreach (var card in storeCards)
                    {
                        if (card != null && !card.IsDisposed && panelStore.Controls.Contains(card))
                        {
                            card.Visible = true;
                        }
                    }
                }

                foreach (var skeleton in skeletonsToDispose)
                {
                    skeleton?.Dispose();
                }
            }
        }
    }
}


