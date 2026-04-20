using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace WatchNow.Avalonia.Views.UserControls
{
    /// <summary>
    /// A tab control that caches content views to prevent them from being unloaded when switching tabs.
    /// Uses TabStrip for headers and a Panel with IsVisible toggling to display cached content.
    /// Views are never removed from the visual tree, which prevents WebView2 from unloading.
    /// Based on https://github.com/stevemonaco/AvaloniaDemos/tree/main/TabStripViewCaching
    /// </summary>
    public partial class CachedTabControl : UserControl
    {
        public static readonly StyledProperty<IEnumerable> ItemsSourceProperty =
            AvaloniaProperty.Register<CachedTabControl, IEnumerable>(nameof(ItemsSource));

        public static readonly StyledProperty<object> SelectedItemProperty =
            AvaloniaProperty.Register<CachedTabControl, object>(nameof(SelectedItem),
                defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
            AvaloniaProperty.Register<CachedTabControl, IDataTemplate>(nameof(ItemTemplate));

        public static readonly StyledProperty<IDataTemplate> CachedContentTemplateProperty =
            AvaloniaProperty.Register<CachedTabControl, IDataTemplate>(nameof(CachedContentTemplate));

        private TabStrip _tabStrip;
        private Panel _contentPanel;
        private readonly Dictionary<object, Control> _viewCache = new();
        private INotifyCollectionChanged _observedCollection;
        private bool _isUpdating;

        public IEnumerable ItemsSource
        {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public IDataTemplate ItemTemplate
        {
            get => GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        /// <summary>
        /// DataTemplate used to create content views. Each view is created once per item and cached.
        /// </summary>
        public IDataTemplate CachedContentTemplate
        {
            get => GetValue(CachedContentTemplateProperty);
            set => SetValue(CachedContentTemplateProperty, value);
        }

        public CachedTabControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _tabStrip = this.FindControl<TabStrip>("tabStrip");
            _contentPanel = this.FindControl<Panel>("tabContent");
            _tabStrip.SelectionChanged += TabStrip_SelectionChanged;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ItemsSourceProperty)
            {
                HandleItemsSourceChanged(change.OldValue as IEnumerable, change.NewValue as IEnumerable);
            }
            else if (change.Property == SelectedItemProperty)
            {
                HandleSelectedItemChanged();
            }
            else if (change.Property == ItemTemplateProperty)
            {
                if (_tabStrip != null)
                {
                    _tabStrip.ItemTemplate = change.NewValue as IDataTemplate;
                }
            }
        }

        private void HandleItemsSourceChanged(IEnumerable oldSource, IEnumerable newSource)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => HandleItemsSourceChanged(oldSource, newSource));
                return;
            }

            if (_observedCollection != null)
            {
                _observedCollection.CollectionChanged -= ItemsSource_CollectionChanged;
                _observedCollection = null;
            }

            if (_contentPanel != null)
            {
                _contentPanel.Children.Clear();
            }
            _viewCache.Clear();

            // Guard against TabStrip auto-selecting an item when ItemsSource is set,
            // which would overwrite the bound SelectedItem value
            _isUpdating = true;
            try
            {
                if (_tabStrip != null)
                {
                    _tabStrip.ItemsSource = newSource;
                }

                if (newSource != null)
                {
                    if (newSource is INotifyCollectionChanged incc)
                    {
                        _observedCollection = incc;
                        incc.CollectionChanged += ItemsSource_CollectionChanged;
                    }
                }

                // Restore the bound SelectedItem after ItemsSource change
                if (SelectedItem != null && _tabStrip != null)
                {
                    _tabStrip.SelectedItem = SelectedItem;
                }

                UpdateVisibility();
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void HandleSelectedItemChanged()
        {
            if (_isUpdating) return;
            _isUpdating = true;
            try
            {
                if (_tabStrip != null)
                {
                    _tabStrip.SelectedItem = SelectedItem;
                }
                UpdateVisibility();
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void TabStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdating) return;
            _isUpdating = true;
            try
            {
                if (_tabStrip.SelectedItem != null)
                {
                    SelectedItem = _tabStrip.SelectedItem;
                    UpdateVisibility();
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }

        /// <summary>
        /// Shows only the selected view by toggling IsVisible on all cached views.
        /// All views remain in the visual tree so WebView2 is never unloaded.
        /// </summary>
        private void UpdateVisibility()
        {
            if (_contentPanel == null) return;

            var selected = SelectedItem;
            Control selectedView = null;
            if (selected != null)
            {
                selectedView = EnsureCached(selected);
            }

            foreach (var child in _contentPanel.Children)
            {
                child.IsVisible = (child == selectedView);
            }
        }

        private Control EnsureCached(object item)
        {
            if (!_viewCache.TryGetValue(item, out var view))
            {
                if (CachedContentTemplate != null)
                {
                    view = CachedContentTemplate.Build(item);
                    if (view != null)
                    {
                        view.DataContext = item;
                        view.IsVisible = false;
                        _viewCache[item] = view;
                        _contentPanel?.Children.Add(view);
                    }
                }
            }
            return view;
        }

        private void RemoveFromCache(object item)
        {
            if (_viewCache.TryGetValue(item, out var view))
            {
                _contentPanel?.Children.Remove(view);
                _viewCache.Remove(item);
            }
        }

        private void ItemsSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => ItemsSource_CollectionChanged(sender, e));
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    // Views are created lazily in UpdateVisibility when a tab is first selected.
                    // This avoids binding to collections that are still being populated on background threads.
                    UpdateVisibility();
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (var item in e.OldItems)
                        {
                            RemoveFromCache(item);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (e.OldItems != null)
                    {
                        foreach (var item in e.OldItems)
                        {
                            RemoveFromCache(item);
                        }
                    }
                    if (e.NewItems != null)
                    {
                        foreach (var item in e.NewItems)
                        {
                            EnsureCached(item);
                        }
                    }
                    UpdateVisibility();
                    break;

                case NotifyCollectionChangedAction.Reset:
                    _contentPanel?.Children.Clear();
                    _viewCache.Clear();
                    if (ItemsSource != null)
                    {
                        foreach (var item in ItemsSource)
                        {
                            EnsureCached(item);
                        }
                    }
                    UpdateVisibility();
                    break;
            }
        }
    }
}
