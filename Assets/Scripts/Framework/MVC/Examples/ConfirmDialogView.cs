using System;
using UnityEngine;
using UnityEngine.UI;

namespace Framework.MVC.Examples
{
    /// <summary>
    /// 示例：确认弹窗View
    /// 展示如何实现一个简单的确认弹窗
    /// </summary>
    public class ConfirmDialogView : UIView<DialogData>
    {
        [Header("UI Components")]
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _contentText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Text _confirmButtonText;
        [SerializeField] private Text _cancelButtonText;

        /// <summary>
        /// 确认事件
        /// </summary>
        public event Action OnConfirmClicked;

        /// <summary>
        /// 取消事件
        /// </summary>
        public event Action OnCancelClicked;

        protected override void OnInit()
        {
            base.OnInit();

            // 绑定按钮事件
            UIBinding.BindButton(_confirmButton, () => OnConfirmClicked?.Invoke());
            UIBinding.BindButton(_cancelButton, () => OnCancelClicked?.Invoke());
        }

        public override void UpdateView(DialogData data)
        {
            if (data == null) return;

            // 更新文本
            UIBinding.BindText(_titleText, data.Title);
            UIBinding.BindText(_contentText, data.Content);
            UIBinding.BindText(_confirmButtonText, data.ConfirmButtonText);
            UIBinding.BindText(_cancelButtonText, data.CancelButtonText);

            // 控制取消按钮显示
            UIBinding.BindActive(_cancelButton?.gameObject, data.ShowCancelButton);
        }

        /// <summary>
        /// 设置回调
        /// </summary>
        public void SetCallbacks(Action onConfirm, Action onCancel)
        {
            OnConfirmClicked -= onConfirm;
            OnCancelClicked -= onCancel;

            OnConfirmClicked += onConfirm;
            OnCancelClicked += onCancel;
        }

        protected override void OnCleanup()
        {
            base.OnCleanup();
            OnConfirmClicked = null;
            OnCancelClicked = null;
        }
    }
}