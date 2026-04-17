using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using doanC_.ViewModels;

namespace doanC_.Services.Localization
{
    /// <summary>
    /// Dùng ?? c?p nh?t t?t c? ViewModels khi ngôn ng? thay ??i
  /// </summary>
    public static class LanguageChangeManager
  {
     private static List<ILanguageRefresh> _viewModels = new List<ILanguageRefresh>();

        /// <summary>
        /// ??ng ký ViewModel ?? nh?n thông báo thay ??i ngôn ng?
        /// </summary>
        public static void Register(ILanguageRefresh viewModel)
        {
if (!_viewModels.Contains(viewModel))
         {
      _viewModels.Add(viewModel);
            }
        }

   /// <summary>
        /// H?y ??ng ký ViewModel
      /// </summary>
    public static void Unregister(ILanguageRefresh viewModel)
        {
            _viewModels.Remove(viewModel);
        }

        /// <summary>
        /// Thông báo t?t c? ViewModel c?p nh?t ngôn ng?
 /// </summary>
   public static void NotifyLanguageChanged()
        {
      foreach (var viewModel in _viewModels)
            {
     viewModel.RefreshLanguage();
            }
        }
    }

    /// <summary>
    /// Interface cho các ViewModel c?n c?p nh?t ngôn ng? ??ng
    /// </summary>
    public interface ILanguageRefresh
    {
        void RefreshLanguage();
    }
}
