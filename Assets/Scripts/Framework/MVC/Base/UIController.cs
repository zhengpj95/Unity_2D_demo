using System;
using UnityEngine;

namespace Framework.MVC
{
    /// <summary>
    /// UI Controller基类
    /// 负责连接Model和View，处理业务逻辑
    /// </summary>
    /// <typeparam name="TData">数据类型</typeparam>
    public abstract class UIController<TData> : IController<TData> where TData : UIData, new()
    {
        /// <summary>
        /// 关联的Model
        /// </summary>
        public IModel<TData> Model => _model;

        /// <summary>
        /// 关联的View
        /// </summary>
        public IView<TData> View => _view;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized { get; protected set; }

        /// <summary>
        /// 是否已启动
        /// </summary>
        public bool IsRunning { get; protected set; }

        /// <summary>
        /// Model实例
        /// </summary>
        protected UIModel<TData> _model;

        /// <summary>
        /// View实例
        /// </summary>
        protected IView<TData> _view;

        /// <summary>
        /// 构造函数
        /// </summary>
        protected UIController()
        {
            _model = CreateModel();
        }

        /// <summary>
        /// 创建Model实例（子类可重写以自定义Model）
        /// </summary>
        protected abstract UIModel<TData> CreateModel();

        /// <summary>
        /// 初始化控制器
        /// </summary>
        /// <param name="view">视图实例</param>
        public virtual void Initialize(IView<TData> view)
        {
            if (IsInitialized)
            {
                Debug.LogWarning($"[{GetType().Name}] Controller already initialized");
                return;
            }

            _view = view;

            // 初始化Model
            _model?.Initialize();

            // 初始化View
            _view?.Initialize();

            // 绑定事件
            BindEvents();

            // 子类初始化
            OnInit();

            IsInitialized = true;
        }

        /// <summary>
        /// 绑定事件
        /// </summary>
        protected virtual void BindEvents()
        {
            if (_model != null)
            {
                _model.OnDataChanged += OnModelDataChanged;
            }

            if (_view != null)
            {
                _view.OnVisibilityChanged += OnViewVisibilityChanged;
            }
        }

        /// <summary>
        /// 解绑事件
        /// </summary>
        protected virtual void UnbindEvents()
        {
            if (_model != null)
            {
                _model.OnDataChanged -= OnModelDataChanged;
            }

            if (_view != null)
            {
                _view.OnVisibilityChanged -= OnViewVisibilityChanged;
            }
        }

        /// <summary>
        /// Model数据变化回调
        /// </summary>
        protected virtual void OnModelDataChanged(TData data)
        {
            _view?.UpdateView(data);
        }

        /// <summary>
        /// View可见性变化回调
        /// </summary>
        protected virtual void OnViewVisibilityChanged(bool visible)
        {
            if (visible)
            {
                OnViewShown();
            }
            else
            {
                OnViewHidden();
            }
        }

        /// <summary>
        /// View显示时调用
        /// </summary>
        protected virtual void OnViewShown() { }

        /// <summary>
        /// View隐藏时调用
        /// </summary>
        protected virtual void OnViewHidden() { }

        /// <summary>
        /// 初始化方法（子类重写）
        /// </summary>
        protected virtual void OnInit() { }

        /// <summary>
        /// 启动控制器
        /// </summary>
        public virtual void Start()
        {
            if (!IsInitialized)
            {
                Debug.LogError($"[{GetType().Name}] Controller not initialized");
                return;
            }

            if (IsRunning) return;

            IsRunning = true;
            OnStart();
        }

        /// <summary>
        /// 启动时调用（子类重写）
        /// </summary>
        protected virtual void OnStart() { }

        /// <summary>
        /// 停止控制器
        /// </summary>
        public virtual void Stop()
        {
            if (!IsRunning) return;

            IsRunning = false;
            OnStop();
        }

        /// <summary>
        /// 停止时调用（子类重写）
        /// </summary>
        protected virtual void OnStop() { }

        /// <summary>
        /// 清理控制器
        /// </summary>
        public virtual void Cleanup()
        {
            Stop();
            UnbindEvents();
            _model?.Cleanup();
            _view?.Cleanup();
            OnCleanup();
            IsInitialized = false;
        }

        /// <summary>
        /// 清理时调用（子类重写）
        /// </summary>
        protected virtual void OnCleanup() { }

        #region 数据操作方法

        /// <summary>
        /// 更新数据
        /// </summary>
        public void UpdateData(TData data)
        {
            _model?.UpdateData(data);
        }

        /// <summary>
        /// 修改数据
        /// </summary>
        public void ModifyData(Action<TData> modifier)
        {
            _model?.ModifyData(modifier);
        }

        /// <summary>
        /// 获取当前数据
        /// </summary>
        public TData GetData()
        {
            return _model?.Data;
        }

        /// <summary>
        /// 重置数据
        /// </summary>
        public void ResetData()
        {
            _model?.ResetData();
        }

        /// <summary>
        /// 显示View
        /// </summary>
        public void ShowView()
        {
            _view?.Show();
        }

        /// <summary>
        /// 隐藏View
        /// </summary>
        public void HideView()
        {
            _view?.Hide();
        }

        #endregion
    }
}