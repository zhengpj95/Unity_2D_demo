using System;
using UnityEngine;

namespace Framework.MVC.Examples
{
    /// <summary>
    /// 示例：确认弹窗Controller
    /// 展示如何实现Controller来管理弹窗逻辑
    /// </summary>
    public class ConfirmDialogController : UIController<DialogData>
    {
        public event Action OnDialogClosed;
        public bool DialogResult { get; private set; }

        protected override UIModel<DialogData> CreateModel()
        {
            return new UIModel<DialogData>();
        }

        protected override void OnInit()
        {
            base.OnInit();

            var view = _view as ConfirmDialogView;
            if (view != null)
            {
                view.OnConfirmClicked += HandleConfirm;
                view.OnCancelClicked += HandleCancel;
            }
        }

        private void HandleConfirm()
        {
            DialogResult = true;
            _model.Data.OnConfirm?.Invoke();
            HideView();
        }

        private void HandleCancel()
        {
            DialogResult = false;
            _model.Data.OnCancel?.Invoke();
            HideView();
        }

        public void ShowDialog(string title, string content, Action onConfirm = null, Action onCancel = null)
        {
            var data = new DialogData
            {
                Title = title,
                Content = content,
                ShowCancelButton = true,
                OnConfirm = onConfirm,
                OnCancel = onCancel
            };

            UpdateData(data);
            ShowView();
        }

        protected override void OnViewHidden()
        {
            base.OnViewHidden();
            OnDialogClosed?.Invoke();
        }

        protected override void OnCleanup()
        {
            var view = _view as ConfirmDialogView;
            if (view != null)
            {
                view.OnConfirmClicked -= HandleConfirm;
                view.OnCancelClicked -= HandleCancel;
            }
            base.OnCleanup();
        }
    }
}
