using CraftifyWPF.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CraftifyWPF
{
    public partial class MainWindow : Window
    {
        private List<Recipe> _recipes;
        private List<int> _favorites;
        private string _currentTab = "recipes";
        private string _selectedCategory;
        private string _selectedFavCategory;
        private Recipe _currentRecipe;
        private int _currentAltIndex;
        private string _searchFilter = "all";
        private string _lastSearch = "";
        private static readonly string FavFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Craftify", "favorites.json");

        public MainWindow()
        {
            InitializeComponent();
            _recipes = RecipeData.Recipes;
            _favorites = LoadFavorites();
            foreach (var r in _recipes)
                r.IsFavorite = _favorites.Contains(r.Id);
            ShowTab("recipes");
        }

        private List<int> LoadFavorites()
        {
            try
            {
                if (File.Exists(FavFile))
                    return System.Text.Json.JsonSerializer.Deserialize<List<int>>(File.ReadAllText(FavFile)) ?? new();
            }
            catch { }
            return new();
        }

        private void SaveFavorites()
        {
            try
            {
                var dir = Path.GetDirectoryName(FavFile);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(FavFile, System.Text.Json.JsonSerializer.Serialize(_favorites));
            }
            catch { }
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
                ShowTab(tag);
        }

        private void ShowTab(string tab)
        {
            _currentTab = tab;
            foreach (var b in new[] { TabRecipes, TabFavorites, TabSearch, TabMore })
                b.Foreground = new SolidColorBrush(b.Tag.ToString() == tab ? _colAccent : _colText2);

            switch (tab)
            {
                case "recipes": ShowRecipesTab(); break;
                case "favorites": ShowFavoritesTab(); break;
                case "search": ShowSearchTab(); break;
                case "more": ShowMoreTab(); break;
            }
        }

        // ==================== Recipes Tab ====================
        private void ShowRecipesTab()
        {
            var sp = new StackPanel { Background = new SolidColorBrush(_colBg) };

            sp.Children.Add(new TextBlock
            {
                Text = "Craftify",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(_colText),
                Margin = new Thickness(16, 12, 16, 2)
            });
            sp.Children.Add(new TextBlock
            {
                Text = "Browse Minecraft crafting recipes",
                FontSize = 12,
                Foreground = new SolidColorBrush(_colText2),
                Margin = new Thickness(16, 0, 16, 8)
            });

            var catPanel = new WrapPanel { Margin = new Thickness(12, 0, 4, 8) };
            var allBtn = CreateCatButton("All", null, true);
            catPanel.Children.Add(allBtn);
            foreach (var cat in RecipeData.Categories)
                catPanel.Children.Add(CreateCatButton(cat, cat, false));
            sp.Children.Add(catPanel);

            _selectedCategory = null;
            sp.Children.Add(BuildRecipeList(null, false));
            TabContent.Content = new ScrollViewer { Content = sp, Background = new SolidColorBrush(_colBg) };
        }

        private Button CreateCatButton(string text, string category, bool isAll)
        {
            var btn = new Button
            {
                Content = text,
                Tag = category,
                Background = new SolidColorBrush(isAll ? _colAccent : _colSurf2),
                Foreground = Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(12, 4, 12, 4),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 4, 4)
            };
            btn.Click += (s, e) =>
            {
                _selectedCategory = category;
                // Rebuild tab
                ShowRecipesTab();
            };
            return btn;
        }

        private FrameworkElement BuildRecipeList(string filterCategory, bool isFavTab)
        {
            var container = new StackPanel();

            var recipes = _recipes.AsEnumerable();
            if (filterCategory != null)
                recipes = recipes.Where(r => r.Category == filterCategory);
            if (isFavTab)
                recipes = recipes.Where(r => _favorites.Contains(r.Id));
            var list = recipes.OrderBy(r => r.Name).ToList();

            var groups = list.GroupBy(r => r.Name[0].ToString().ToUpper())
                             .OrderBy(g => g.Key);

            foreach (var group in groups)
            {
                container.Children.Add(new TextBlock
                {
                    Text = group.Key,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(_colText2),
                    Margin = new Thickness(12, 8, 0, 2)
                });
                foreach (var recipe in group.OrderBy(r => r.Name))
                {
                    var card = CreateRecipeCard(recipe);
                    container.Children.Add(card);
                }
            }

            if (list.Count == 0)
            {
                container.Children.Add(new TextBlock
                {
                    Text = isFavTab ? "No favorite recipes yet. Tap the heart to add some!" : "No recipes found.",
                    Foreground = new SolidColorBrush(_colText2),
                    FontSize = 14,
                    Margin = new Thickness(16, 20, 16, 20),
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
            }

            return container;
        }

        private Border CreateRecipeCard(Recipe recipe)
        {
            var border = new Border
            {
                Margin = new Thickness(8, 2, 8, 2),
                Cursor = Cursors.Hand,
                Tag = recipe
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var img = new Image { Width = 44, Height = 44, Margin = new Thickness(4) };
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", $"{recipe.Image}.png");
            if (File.Exists(path))
                img.Source = new BitmapImage(new Uri(path));
            else
            {
                var border2 = new Border
                {
                    Width = 44, Height = 44,
                    Background = new SolidColorBrush(_colSurf2),
                    CornerRadius = new CornerRadius(6),
                    Child = new TextBlock
                    {
                        Text = recipe.Name[0].ToString(),
                        Foreground = new SolidColorBrush(_colText),
                        FontSize = 18,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                Grid.SetColumn(border2, 0);
                grid.Children.Add(border2);
            }
            Grid.SetColumn(img, 0);
            grid.Children.Add(img);

            var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
            infoStack.Children.Add(new TextBlock
            {
                Text = recipe.Name + (_favorites.Contains(recipe.Id) ? " \u2665" : ""),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(_colText)
            });
            infoStack.Children.Add(new TextBlock
            {
                Text = recipe.Category,
                FontSize = 11,
                Foreground = new SolidColorBrush(_colText2)
            });
            Grid.SetColumn(infoStack, 1);
            grid.Children.Add(infoStack);

            var arrow = new TextBlock
            {
                Text = "\u203A",
                FontSize = 18,
                Foreground = new SolidColorBrush(_colText2),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(arrow, 2);
            grid.Children.Add(arrow);

            border.Child = grid;
            border.MouseDown += (s, e) => OpenDetail(recipe);
            return border;
        }

        // ==================== Favorites Tab ====================
        private void ShowFavoritesTab()
        {
            var sp = new StackPanel { Background = new SolidColorBrush(_colBg) };

            sp.Children.Add(new TextBlock
            {
                Text = "Favorite Recipes",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(_colText),
                Margin = new Thickness(16, 12, 16, 2)
            });
            sp.Children.Add(new TextBlock
            {
                Text = "Your saved recipes",
                FontSize = 12,
                Foreground = new SolidColorBrush(_colText2),
                Margin = new Thickness(16, 0, 16, 8)
            });

            var favRecipes = _recipes.Where(r => _favorites.Contains(r.Id)).ToList();
            var cats = favRecipes.Select(r => r.Category).Distinct().OrderBy(c => c).ToList();
            var catPanel = new WrapPanel { Margin = new Thickness(12, 0, 4, 8) };
            var allBtn = new Button
            {
                Content = "All",
                Tag = null,
                Background = new SolidColorBrush(_colAccent),
                Foreground = Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(12, 4, 12, 4),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 4, 4)
            };
            allBtn.Click += (s, e) => { _selectedFavCategory = null; ShowFavoritesTab(); };
            catPanel.Children.Add(allBtn);

            foreach (var cat in cats)
            {
                var btn = new Button
                {
                    Content = cat,
                    Tag = cat,
                    Background = new SolidColorBrush(_selectedFavCategory == cat ? _colAccent : _colSurf2),
                    Foreground = Brushes.White,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Padding = new Thickness(12, 4, 12, 4),
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    Margin = new Thickness(0, 0, 4, 4)
                };
                btn.Click += (s, e) => { _selectedFavCategory = cat; ShowFavoritesTab(); };
                catPanel.Children.Add(btn);
            }
            sp.Children.Add(catPanel);
            sp.Children.Add(BuildRecipeList(_selectedFavCategory, true));
            TabContent.Content = new ScrollViewer { Content = sp, Background = new SolidColorBrush(_colBg) };
        }

        // ==================== Search Tab ====================
        private void ShowSearchTab()
        {
            var sp = new StackPanel { Background = new SolidColorBrush(_colBg) };
            sp.Children.Add(new TextBlock
            {
                Text = "Search",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(_colText),
                Margin = new Thickness(16, 12, 16, 2)
            });

            var searchBox = new TextBox
            {
                Margin = new Thickness(16, 4, 16, 8),
                Padding = new Thickness(10, 8, 10, 8),
                FontSize = 14,
                Background = new SolidColorBrush(_colSurf),
                Foreground = new SolidColorBrush(_colText),
                BorderBrush = new SolidColorBrush(_colBorder),
                BorderThickness = new Thickness(1),
                CaretBrush = new SolidColorBrush(_colAccent),
                Text = _lastSearch
            };
            searchBox.TextChanged += (s, e) =>
            {
                _lastSearch = searchBox.Text;
                UpdateSearchResults(sp, searchBox.Text);
            };
            sp.Children.Add(searchBox);

            var filterPanel = new WrapPanel { Margin = new Thickness(12, 0, 4, 8) };
            var allBtn = new Button
            {
                Content = "All recipes",
                Tag = "all",
                Background = new SolidColorBrush(_searchFilter == "all" ? _colAccent : _colSurf2),
                Foreground = Brushes.White,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(12, 4, 12, 4),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 4, 4)
            };
            allBtn.Click += (s, e) => { _searchFilter = "all"; ShowSearchTab(); };
            filterPanel.Children.Add(allBtn);

            var favBtn = new Button
            {
                Content = "Favorites",
                Tag = "favorites",
                Background = new SolidColorBrush(_searchFilter == "favorites" ? _colAccent : _colSurf2),
                Foreground = Brushes.White,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(12, 4, 12, 4),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 4, 4)
            };
            favBtn.Click += (s, e) => { _searchFilter = "favorites"; ShowSearchTab(); };
            filterPanel.Children.Add(favBtn);
            sp.Children.Add(filterPanel);

            var resultPanel = new StackPanel();
            sp.Children.Add(resultPanel);
            TabContent.Content = new ScrollViewer { Content = sp, Background = new SolidColorBrush(_colBg) };
            UpdateSearchResults(sp, _lastSearch);
        }

        private void UpdateSearchResults(StackPanel parent, string query)
        {
            if (parent == null) return;
            var resultPanel = parent.Children[parent.Children.Count - 1] as StackPanel;
            resultPanel?.Children.Clear();

            var q = query.Trim().ToLower();
            if (string.IsNullOrEmpty(q))
            {
                resultPanel?.Children.Add(new TextBlock
                {
                    Text = "Search for Recipes\nFind recipes by name, category, or ingredients.",
                    Foreground = new SolidColorBrush(_colText2),
                    FontSize = 14,
                    Margin = new Thickness(16, 20, 16, 20),
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                return;
            }

            var results = _recipes.Where(r =>
                r.Name.ToLower().Contains(q) ||
                r.Category.ToLower().Contains(q) ||
                r.Ingredients.Any(i => i.ToLower().Contains(q))
            );
            if (_searchFilter == "favorites")
                results = results.Where(r => _favorites.Contains(r.Id));

            var list = results.OrderBy(r => r.Name).ToList();
            if (list.Count == 0)
            {
                resultPanel?.Children.Add(new TextBlock
                {
                    Text = "No recipes found. Try adjusting your search term.",
                    Foreground = new SolidColorBrush(_colText2),
                    FontSize = 14,
                    Margin = new Thickness(16, 20, 16, 20),
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                return;
            }

            var groups = list.GroupBy(r => r.Name[0].ToString().ToUpper()).OrderBy(g => g.Key);
            foreach (var group in groups)
            {
                resultPanel?.Children.Add(new TextBlock
                {
                    Text = group.Key,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(_colText2),
                    Margin = new Thickness(12, 8, 0, 2)
                });
                foreach (var recipe in group.OrderBy(r => r.Name))
                    resultPanel?.Children.Add(CreateRecipeCard(recipe));
            }
        }

        // ==================== More Tab ====================
        private void ShowMoreTab()
        {
            var sp = new StackPanel { Background = new SolidColorBrush(_colBg) };
            sp.Children.Add(new TextBlock
            {
                Text = "More",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(_colText),
                Margin = new Thickness(16, 12, 16, 2)
            });

            AddSectionTitle(sp, "Data Sync & Status");

            Border syncBtn = null;
            syncBtn = CreateMoreItem("Sync Recipes", "\u25B6", () =>
            {
                var idx = sp.Children.IndexOf(syncBtn) + 1;
                if (idx < sp.Children.Count && sp.Children[idx] is TextBlock status)
                    status.Text = $"{_recipes.Count} recipes loaded (offline mode)";
            });
            sp.Children.Add(syncBtn);
            sp.Children.Add(new TextBlock
            {
                Text = $"{_recipes.Count} recipes available (offline)",
                FontSize = 12,
                Foreground = new SolidColorBrush(_colText2),
                Margin = new Thickness(16, 2, 16, 4)
            });

            AddSectionTitle(sp, "About");
            var aboutBtn = CreateMoreItem("About Craftify", "\u203A", () =>
            {
                MessageBox.Show(
                    "Craftify for Minecraft\n\nVersion 1.0 (WPF)\n\nBrowse Minecraft crafting recipes.\n\nNot an official Minecraft product.\nNot associated with Mojang or Microsoft.\n\nData sourced from the Minecraft Wiki.",
                    "About Craftify",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });
            sp.Children.Add(aboutBtn);

            var themeBtn = CreateMoreItem("Toggle Theme", "\u203A", ToggleTheme);
            sp.Children.Add(themeBtn);

            sp.Children.Add(new TextBlock
            {
                Text = "Craftify is not an official Minecraft product. Not associated with Mojang or Microsoft.",
                FontSize = 11,
                Foreground = new SolidColorBrush(_colText2),
                Margin = new Thickness(16, 12, 16, 8),
                TextWrapping = TextWrapping.Wrap
            });

            TabContent.Content = new ScrollViewer { Content = sp, Background = new SolidColorBrush(_colBg) };
        }

        private void AddSectionTitle(StackPanel parent, string title)
        {
            parent.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(_colText2),
                Margin = new Thickness(16, 12, 16, 4)
            });
        }

        private Border CreateMoreItem(string text, string arrow, Action click)
        {
            var border = new Border
            {
                Margin = new Thickness(12, 2, 12, 2),
                Cursor = Cursors.Hand,
                Background = new SolidColorBrush(_colSurf),
                CornerRadius = new CornerRadius(4),
                BorderBrush = new SolidColorBrush(_colBorder),
                BorderThickness = new Thickness(1)
            };
            var grid = new Grid { Margin = new Thickness(12, 10, 12, 10) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            grid.Children.Add(new TextBlock
            {
                Text = text,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(_colText),
                VerticalAlignment = VerticalAlignment.Center
            });
            var arrowBlock = new TextBlock
            {
                Text = arrow,
                FontSize = 16,
                Foreground = new SolidColorBrush(_colText2),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(arrowBlock, 1);
            grid.Children.Add(arrowBlock);
            border.Child = grid;
            border.MouseDown += (s, e) => click();
            return border;
        }

        // ==================== Theme ====================
        private bool _isDark = true;
        private Color _colBg => ColorFromHex(_isDark ? "#0d0d12" : "#f5f5f8");
        private Color _colSurf => ColorFromHex(_isDark ? "#1a1a24" : "#ffffff");
        private Color _colSurf2 => ColorFromHex(_isDark ? "#242436" : "#eeeef2");
        private Color _colText => ColorFromHex(_isDark ? "#e8e8f0" : "#1a1a2e");
        private Color _colText2 => ColorFromHex(_isDark ? "#9090a8" : "#666680");
        private Color _colBorder => ColorFromHex(_isDark ? "#2a2a3a" : "#dddde8");
        private Color _colAccent => ColorFromHex("#6c5ce7");
        private Color _colRed => ColorFromHex("#ff6b6b");
        private Color _colEmpty => ColorFromHex(_isDark ? "#3a3a4a" : "#cccccc");

        private static Color ColorFromHex(string hex) =>
            (Color)ColorConverter.ConvertFromString(hex);

        private void ToggleTheme()
        {
            _isDark = !_isDark;
            Background = new SolidColorBrush(_colBg);
            ShowTab(_currentTab);
        }

        // ==================== Detail ====================
        private void OpenDetail(Recipe recipe)
        {
            _currentRecipe = recipe;
            _currentAltIndex = 0;
            DetailTitle.Text = recipe.Name;
            UpdateHeartButton();
            RenderDetail();
            DetailOverlay.Visibility = Visibility.Visible;
        }

        private void UpdateHeartButton()
        {
            if (_currentRecipe == null) return;
            var isFav = _favorites.Contains(_currentRecipe.Id);
            DetailHeart.Content = isFav ? "\u2665" : "\u2661";
            DetailHeart.Foreground = new SolidColorBrush(isFav ? _colRed : _colText2);
        }

        private void RenderDetail()
        {
            DetailPanel.Children.Clear();
            var r = _currentRecipe;
            if (r == null) return;

            // Alt recipe options
            var altPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
            altPanel.Children.Add(new Button
            {
                Content = "Recipe 1",
                Tag = 0,
                Background = new SolidColorBrush(_currentAltIndex == 0 ? _colAccent : _colSurf2),
                Foreground = Brushes.White,
                FontSize = 11, FontWeight = FontWeights.Bold,
                Padding = new Thickness(12, 4, 12, 4), BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 4, 4)
            });
            ((Button)altPanel.Children[0]).Click += (s, e) => { _currentAltIndex = 0; RenderDetail(); };
            DetailPanel.Children.Add(altPanel);

            // Crafting grid
            DetailPanel.Children.Add(BuildCraftingGrid(r, _currentAltIndex));

            // Category
            if (!string.IsNullOrEmpty(r.Category))
                DetailPanel.Children.Add(new Border
                {
                    Background = new SolidColorBrush(_colSurf2),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(10, 4, 10, 4),
                    Margin = new Thickness(0, 8, 0, 4),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Child = new TextBlock
                    {
                        Text = r.Category,
                        FontSize = 12,
                        Foreground = new SolidColorBrush(_colText2)
                    }
                });
        }

        private FrameworkElement BuildCraftingGrid(Recipe recipe, int altIdx)
        {
            var container = new StackPanel();

            var gridRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 8) };

            var ings = GetIngredients(recipe);
            var isFurnace = recipe.IsFurnace;

            // Ingredients grid
            var grid = new Grid();
            double cellSize = isFurnace ? 56 : 48;
            double gap = 4;

            if (isFurnace)
            {
                grid.Width = cellSize;
                grid.Height = cellSize * 2 + gap;
                for (int i = 0; i < 2; i++)
                {
                    var cell = CreateIngredientCell(i < ings.Count ? ings[i] : "", cellSize);
                    Grid.SetRow(cell, i);
                    grid.Children.Add(cell);
                }
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(cellSize) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(gap) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(cellSize) });
            }
            else
            {
                grid.Width = cellSize * 3 + gap * 2;
                grid.Height = cellSize * 3 + gap * 2;
                for (int r = 0; r < 3; r++)
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(cellSize) });
                    for (int c = 0; c < 3; c++)
                    {
                        if (r == 0) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cellSize) });
                        int idx = r * 3 + c;
                        var cell = CreateIngredientCell(idx < ings.Count ? ings[idx] : "", cellSize);
                        Grid.SetRow(cell, r);
                        Grid.SetColumn(cell, c);
                        grid.Children.Add(cell);
                    }
                }
            }
            gridRow.Children.Add(grid);

            // Arrow
            gridRow.Children.Add(new TextBlock
            {
                Text = "\u2192",
                FontSize = 28,
                Foreground = new SolidColorBrush(_colText2),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 12, 0)
            });

            // Output
            var outputStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var outCell = CreateIngredientCell(recipe.Name, cellSize, true);
            outCell.Width = cellSize + 10;
            outCell.Height = cellSize + 10;
            outputStack.Children.Add(outCell);
            outputStack.Children.Add(new TextBlock
            {
                Text = $"x{recipe.Output}",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(_colText),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            });
            gridRow.Children.Add(outputStack);
            container.Children.Add(gridRow);

            return container;
        }

        private List<string> GetIngredients(Recipe recipe)
        {
            var ings = recipe.Ingredients?.Where(i => !string.IsNullOrEmpty(i)).ToList() ?? new();
            int max = recipe.IsFurnace ? 2 : 9;
            while (ings.Count < max) ings.Add("");
            return ings.Take(max).ToList();
        }

        private Border CreateIngredientCell(string itemName, double size, bool isOutput = false)
        {
            var hasItem = !string.IsNullOrEmpty(itemName);
            var border = new Border
            {
                Width = size + (isOutput ? 10 : 0),
                Height = size + (isOutput ? 10 : 0),
                Background = new SolidColorBrush(hasItem ? _colSurf2 : _colSurf),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(2),
                BorderBrush = new SolidColorBrush(_colBorder),
                BorderThickness = new Thickness(1),
                Cursor = hasItem ? Cursors.Hand : Cursors.Arrow
            };

            if (hasItem)
            {
                var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", $"{itemName.Replace(" ", "_")}.png");
                if (File.Exists(path))
                {
                    var img = new Image
                    {
                        Source = new BitmapImage(new Uri(path)),
                        Width = size - 8,
                        Height = size - 8
                    };
                    border.Child = img;
                }
                else
                {
                    border.Child = new TextBlock
                    {
                        Text = itemName[0].ToString(),
                        FontSize = size * 0.35,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(_colText),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }

                border.MouseDown += (s, e) =>
                {
                    var recipe = _recipes.FirstOrDefault(r => r.Name == itemName);
                    if (recipe != null)
                        OpenDetail(recipe);
                    else
                        MessageBox.Show(itemName, "Ingredient", MessageBoxButton.OK, MessageBoxImage.Information);
                };
            }
            else
            {
                border.Child = new TextBlock
                {
                    Text = "\u25A1",
                    FontSize = size * 0.4,
                    Foreground = new SolidColorBrush(_colEmpty),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            return border;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            DetailOverlay.Visibility = Visibility.Collapsed;
            ShowTab(_currentTab);
        }

        private void ToggleFav_Click(object sender, RoutedEventArgs e)
        {
            if (_currentRecipe == null) return;
            if (_favorites.Contains(_currentRecipe.Id))
                _favorites.Remove(_currentRecipe.Id);
            else
                _favorites.Add(_currentRecipe.Id);
            _currentRecipe.IsFavorite = _favorites.Contains(_currentRecipe.Id);
            SaveFavorites();
            UpdateHeartButton();
            ShowTab(_currentTab);
        }
    }
}
