#pragma warning disable 169

namespace CodeStage.AdvancedFPSCounter
{
	using System.Collections.Generic;

	using CountersData;
	using Labels;

	using UnityEngine;
	using UnityEngine.UI;
	using Utils;
#if UNITY_5_4_OR_NEWER
	using UnityEngine.SceneManagement;
#endif

	/// <summary>
	/// Allows to see frames per second counter, memory usage counter and some simple hardware information right in running app on any device.<br/>
	/// Just call AFPSCounter.AddToScene() to use it.
	/// </summary>
	/// You also may add it to GameObject (without any child or parent objects, with zero rotation, zero position and 1,1,1 scale) as usual or through the<br/>
	/// "GameObject > Create Other > Code Stage > Advanced FPS Counter" menu.
	[AddComponentMenu(MenuPath)]
	[DisallowMultipleComponent]
	[HelpURL("http://codestage.net/uas_files/afps/api/class_code_stage_1_1_advanced_f_p_s_counter_1_1_a_f_p_s_counter.html")]
	public class AFPSCounter : MonoBehaviour
	{
		// ----------------------------------------------------------------------------
		// constants
		// ----------------------------------------------------------------------------
		private const string MenuPath = "Code Stage/🚀 Advanced FPS Counter";
		private const string ComponentName = "Advanced FPS Counter";

		internal const string LogPrefix = "<b>[AFPSCounter]:</b> ";
		internal const char NewLine = '\n';
		internal const char Space = ' ';

		// ----------------------------------------------------------------------------
		// public fields
		// ----------------------------------------------------------------------------

		/// <summary>
		/// Frames Per Second counter.
		/// </summary>
		public FPSCounterData fpsCounter = new FPSCounterData();

		/// <summary>
		/// Mono or heap memory counter.
		/// </summary>
		public MemoryCounterData memoryCounter = new MemoryCounterData();

		/// <summary>
		/// Device hardware info.<br/>
		/// Shows CPU name, cores (threads) count, GPU name, total VRAM, total RAM, screen DPI and screen size.
		/// </summary>
		public DeviceInfoCounterData deviceInfoCounter = new DeviceInfoCounterData();

		/// <summary>
		/// Used to enable / disable plugin at runtime. Set to KeyCode.None to disable.
		/// </summary>
		[Tooltip("Used to enable / disable plugin at runtime.\nSet to None to disable.")]
		public KeyCode hotKey = KeyCode.BackQuote;

		/// <summary>
		/// Used to enable / disable plugin at runtime. Make two circle gestures with your finger \ mouse to switch plugin on and off.
		/// </summary>
		[Tooltip("Used to enable / disable plugin at runtime.\nMake two circle gestures with your finger \\ mouse to switch plugin on and off.")]
		public bool circleGesture;

		/// <summary>
		/// Hot key modifier: any Control on Windows or any Command on Mac.
		/// </summary>
		[Tooltip("Hot key modifier: any Control on Windows or any Command on Mac.")]
		public bool hotKeyCtrl;

		/// <summary>
		/// Hot key modifier: any Shift.
		/// </summary>
		[Tooltip("Hot key modifier: any Shift.")]
		public bool hotKeyShift;

		/// <summary>
		/// Hot key modifier: any Alt.
		/// </summary>
		[Tooltip("Hot key modifier: any Alt.")]
		public bool hotKeyAlt;

		[Tooltip("Prevents current or other topmost Game Object from destroying on level (scene) load.\nApplied once, on Start phase.")]
		[SerializeField]
		private bool keepAlive = true;

		// ----------------------------------------------------------------------------
		// private fields
		// ----------------------------------------------------------------------------

		private Canvas canvas;
		private CanvasScaler canvasScaler;
		private bool externalCanvas;

		private DrawableLabel[] labels;

		private int anchorsCount;
		private int cachedVSync = -1;
		private int cachedFrameRate = -1;
		private bool inited;

		/* circle gesture variables */

		private readonly List<Vector2> gesturePoints = new List<Vector2>();
		private int gestureCount;

		// ----------------------------------------------------------------------------
		// properties
		// ----------------------------------------------------------------------------

		/// <summary>
		/// Read-only property allowing to figure out current keepAlive state.
		/// </summary>
		public bool KeepAlive
		{
			get { return keepAlive; }
		}

		#region OperationMode
		[Tooltip("Disabled: removes labels and stops all internal processes except Hot Key listener.\n\n" +
				 "Background: removes labels keeping counters alive; use for hidden performance monitoring.\n\n" +
				 "Normal: shows labels and runs all internal processes as usual.")]
		[SerializeField]
		private OperationMode operationMode = OperationMode.Normal;

		/// <summary>
		/// Use it to change %AFPSCounter operation mode.
		/// </summary>
		/// Disabled: removes labels and stops all internal processes except Hot Key listener.<br/>
		/// Background: removes labels keeping counters alive. May be useful for hidden performance monitoring and benchmarking. Hot Key has no effect in this mode.<br/>
		/// Normal: shows labels and runs all internal processes as usual.
		public OperationMode OperationMode
		{
			get { return operationMode; }
			set
			{
				if (operationMode == value || !Application.isPlaying) return;
				operationMode = value;

				if (operationMode != OperationMode.Disabled)
				{
					if (operationMode == OperationMode.Background)
					{
						for (var i = 0; i < anchorsCount; i++)
						{
							labels[i].Clear();
						}
					}

					OnEnable();

					fpsCounter.UpdateValue();
					memoryCounter.UpdateValue();
					deviceInfoCounter.UpdateValue();

					UpdateTexts();
				}
				else
				{
					OnDisable();
				}
			}
		}
		#endregion

		#region ForceFrameRate
		[Tooltip("Allows to see how your game performs on specified frame rate.\n" +
				 "Does not guarantee selected frame rate. Set -1 to render as fast as possible in current conditions.\n" +
				 "IMPORTANT: this option disables VSync while enabled!")]
		[SerializeField]
		private bool forceFrameRate;

		/// <summary>
		/// Allows to see how your game performs on specified frame rate.<br/>
		/// <strong>\htmlonly<font color="7030A0">IMPORTANT:</font>\endhtmlonly this option disables VSync while enabled!</strong>
		/// </summary>
		/// Useful to check how physics performs on slow devices for example.
		public bool ForceFrameRate
		{
			get { return forceFrameRate; }
			set
			{
				if (forceFrameRate == value || !Application.isPlaying) return;
				forceFrameRate = value;
				if (operationMode == OperationMode.Disabled) return;

				RefreshForcedFrameRate();
			}
		}
		#endregion

		#region ForcedFrameRate
		[Range(-1, 200)]
		[SerializeField]
		private int forcedFrameRate = -1;

		/// <summary>
		/// Desired frame rate for ForceFrameRate option, does not guarantee selected frame rate.
		/// Set to -1 to render as fast as possible in current conditions.
		/// </summary>
		public int ForcedFrameRate
		{
			get { return forcedFrameRate; }
			set
			{
				if (forcedFrameRate == value || !Application.isPlaying) return;
				forcedFrameRate = value;
				if (operationMode == OperationMode.Disabled) return;

				RefreshForcedFrameRate();
			}
		}
		#endregion

		/* look and feel settings */

		#region Background
		[Tooltip("Background for all texts. Cheapest effect. Overhead: 1 Draw Call.")]
		[SerializeField]
		private bool background = true;

		/// <summary>
		/// Background for all texts.
		/// </summary>
		public bool Background
		{
			get { return background; }
			set
			{
				if (background == value || !Application.isPlaying) return;
				background = value;

				if (operationMode == OperationMode.Disabled || labels == null) return;

				for (var i = 0; i < anchorsCount; i++)
				{
					labels[i].ChangeBackground(background);
				}
			}
		}
		#endregion

		#region BackgroundColor
		[Tooltip("Color of the background.")]
		[SerializeField]
		private Color backgroundColor = new Color32(0, 0, 0, 155);

		/// <summary>
		/// Color of the background.
		/// </summary>
		public Color BackgroundColor
		{
			get { return backgroundColor; }
			set
			{
				if (backgroundColor == value || !Application.isPlaying) return;
				backgroundColor = value;

				if (operationMode == OperationMode.Disabled || labels == null) return;

				for (var i = 0; i < anchorsCount; i++)
				{
					labels[i].ChangeBackgroundColor(backgroundColor);
				}
			}
		}
		#endregion

		#region BackgroundPadding
		[Tooltip("Padding of the background.")]
		[Range(0, 30)]
		[SerializeField]
		private int backgroundPadding = 5;

		/// <summary>
		/// Padding of the background. Change forces the HorizontalLayoutGroup.SetLayoutHorizontal() call.
		/// </summary>
		public int BackgroundPadding
		{
			get { return backgroundPadding; }
			set
			{
				if (backgroundPadding == value || !Application.isPlaying) return;
				backgroundPadding = value;

				if (operationMode == OperationMode.Disabled || labels == null) return;

				for (var i = 0; i < anchorsCount; i++)
				{
					labels[i].ChangeBackgroundPadding(backgroundPadding);
				}
			}
		}
		#endregion

		#region Shadow
		[Tooltip("Shadow effect for all texts. This effect uses extra resources. Overhead: medium CPU and light GPU usage.")]
		[SerializeField]
		private bool shadow;

		/// <summary>
		/// Shadow effect for all texts.
		/// </summary>
		public bool Shadow
		{
			get { return shadow; }
			set
			{
				if (shadow == value || !Application.isPlaying) return;
				shadow = value;

				if (operationMode == OperationMode.Disabled || labels == null) return;

				for (var i = 0; i < anchorsCount; i++)
				{
					labels[i].ChangeShadow(shadow);
				}
			}
		}
		#endregion

		#region ShadowColor
		[Tooltip("Color of the shadow effect.")]
		[SerializeField]
		private Color shadowColor = new Color32(0, 0, 0, 128);

		/// <summary>
		/// Color of the shadow effect.
		/// </summary>
		public Color ShadowColor
		{
			get { return shadowColor; }
			set
			{
				if (shadowColor == value || !Application.isPlaying) return;
				shadowColor = value;

				if (operationMode == OperationMode.Disabled || labels == null) return;

				for (var i = 0; i < anchorsCount; i++)
				{
					labels[i].ChangeShadowColor(shadowColor);
				}
			}
		}
		#endregion

		#region ShadowDistance
		[Tooltip("Distance of the shadow effect.")]
		[SerializeField]
		private Vector2 shadowDistance = new Vector2(1, -1);

		/// <summary>
		/// Distance of the shadow effect.
		/// </summary>
		public Vector2 ShadowDistance
		{
			get { return shadowDistance; }
			set
			{
				if (shadowDistance == value || !Application.isPlaying) return;
				shadowDistance = value;

				if (operationMode == OperationMode.Disabled || labels == null) return;

				for (var i = 0; i < anchorsCount; i++)
				{
					labels[i].ChangeShadowDistance(shadowDistance);
				}
			}
		}
		#endregion

		#region Outline
		[Tooltip("Outline effect for all texts. Resource-heaviest effect. Overhead: huge CPU and medium GPU usage. Not recommended for use unless really necessary.")]
		[SerializeField]
		private bool outline;

		/// <summary>
		/// Outline effect for all texts.
		/// </summary>
		public bool Outline
		{
			get { return outline; }
			set
			{
				if (outline == value || !Application.isPlaying) return;
				outline = value;

				if (operationMode == OperationMode.Disabled || labels == null) return;

				for (var i = 0; i < anchorsCount; i++)
				{
					labels[i].ChangeOutline(outline);
				}
			}
		}
		#endregion

		#region OutlineColor
		[Tooltip("Color of the outline effect.")]
		[SerializeField]
		private Color outlineColor = new Color32(0, 0, 0, 128);

		/// <summary>
		/// Color of the outline effect.
		/// </summary>
		public Color OutlineColor
		{
			get { return outlineColor; }
			set
			{
				if (outlineColor == value || !Application.isPlaying) return;
				outlineColor = value;

				if (operationMode == OperationMode.Disabled || labels == null) return;

				for (var i = 0; i < anchorsCount; i++)
				{
					labels[i].ChangeOutlineColor(outlineColor);
				}
			}
		}
		#endregion

		#region OutlineDistance
		[Tooltip("Distance of the outline effect.")]
		[SerializeField]
		private Vector2 outlineDistance = new Vector2(1, -1);

		/// <summary>
		/// Distance of the outline effect.
		/// </summary>
		public Vector2 OutlineDistance
		{
			get { return outlineDistance; }
			set
			{
				if (outlineDistance == value || !Application.isPlaying) return;
				outlineDistance = value;

				if (operationMode == OperationMode.Disabled || labels == null) return;

				for (var i = 0; i < anchorsCount; i++)
				{
					labels[i].ChangeOutlineDistance(outlineDistance);
				}
			}
		}
		#endregion

		#region AutoScale
		[Tooltip("Controls own Canvas Scaler scale mode. Check to use ScaleWithScreenSize. Otherwise ConstantPixelSize will be used.")]
		[SerializeField]
		private bool autoScale;

		/// <summary>
		/// Controls own Canvas Scaler scale mode. Check to use ScaleWithScreenSize. Otherwise ConstantPixelSize will be used.
		/// </summary>
		public bool AutoScale
		{
			get { return autoScale; }
			set
			{
				if (autoScale == value || !Application.isPlaying) return;
				autoScale = value;

				if (operationMode == OperationMode.Disabled || labels == null) return;
				if (canvasScaler == null) return;

				canvasScaler.uiScaleMode = autoScale
					? CanvasScaler.ScaleMode.ScaleWithScreenSize
					: CanvasScaler.ScaleMode.ConstantPixelSize;
			}
		}
		#endregion

		#region ScaleFactor
		[Tooltip("Controls global scale of all texts.")]
		[Range(0f, 30f)]
		[SerializeField]
		private float scaleFactor = 1;

		/// <summary>
		/// Controls global scale of all texts.
		/// </summary>
		public float ScaleFactor
		{
			get { return scaleFactor; }
			set
			{
				if (System.Math.Abs(scaleFactor - value) < 0.001f || !Application.isPlaying) return;
				scaleFactor = value;
				if (operationMode == OperationMode.Disabled || canvasScaler == null) return;

				canvasScaler.scaleFactor = scaleFactor;
			}
		}
		#endregion

		#region LabelsFont
		[Tooltip("Leave blank to use default font.")]
		[SerializeField]
		private Font labelsFont;

		/// <summary>
		/// Font to render labels with.
		/// </summary>
		public Font LabelsFont
		{
			get { return labelsFont; }
			set
			{
				if (labelsFont == value || !Application.isPlaying) return;
				labelsFont = value;
				if (operationMode == OperationMode.Disabled || labels == null) return;

				for (var i = 0; i < anchorsCount; i++)
				{
					labels[i].ChangeFont(labelsFont);
				}
			}
		}
		#endregion

		#region FontSize
		[Tooltip("Set to 0 to use font size specified in the font importer.")]
		[Range(0, 100)]
		[SerializeField]
		private int fontSize = 14;

		/// <summary>
		/// The font size to use (for dynamic fonts).
		/// </summary>
		/// If this is set to a non-zero value, the font size specified in the font importer is overridden with a custom size. This is only supported for fonts set to use dynamic font rendering. Other fonts will always use the default font size.
		public int FontSize
		{
			get { return fontSize; }
			set
			{
				if (fontSize == value || !Application.isPlaying) return;
				fontSize = value;
				if (operationMode == OperationMode.Disabled || labels == null) return;

				for (var i = 0; i < anchorsCount; i++)
				{
					labels[i].ChangeFontSize(fontSize);
				}
			}
		}
		#endregion

		#region LineSpacing
		[Tooltip("Space between lines in labels.")]
		[Range(0f, 10f)]
		[SerializeField]
		private float lineSpacing = 1;

		/// <summary>
		/// Space between lines.
		/// </summary>
		public float LineSpacing
		{
			get { return lineSpacing; }
			set
			{
				if (System.Math.Abs(lineSpacing - value) < 0.001f || !Application.isPlaying) return;
				lineSpacing = value;
				if (operationMode == OperationMode.Disabled || labels == null) return;

				for (var i = 0; i < anchorsCount; i++)
				{
					labels[i].ChangeLineSpacing(lineSpacing);
				}
			}
		}
		#endregion

		#region CountersSpacing
		[Tooltip("Lines count between different counters in a single label.")]
		[Range(0, 10)]
		[SerializeField]
		private int countersSpacing;

		/// <summary>
		/// Lines count between different counters in a single label.
		/// </summary>
		public int CountersSpacing
		{
			get { return countersSpacing; }
			set
			{
				if (countersSpacing == value || !Application.isPlaying) return;
				countersSpacing = value;
				if (operationMode == OperationMode.Disabled || labels == null) return;

				UpdateTexts();
				for (var i = 0; i < anchorsCount; i++)
				{
					labels[i].dirty = true;
				}
			}
		}
		#endregion

		#region PaddingOffset
		[Tooltip("Pixel offset for anchored labels. Automatically applied to all labels.")]
		[SerializeField]
		private Vector2 paddingOffset = new Vector2(5, 5);

		/// <summary>
		/// Pixel offset for anchored labels. Automatically applied to all labels.
		/// </summary>
		public Vector2 PaddingOffset
		{
			get { return paddingOffset; }
			set
			{
				if (paddingOffset == value || !Application.isPlaying) return;
				paddingOffset = value;
				if (operationMode == OperationMode.Disabled || labels == null) return;

				for (var i = 0; i < anchorsCount; i++)
				{
					labels[i].ChangeOffset(paddingOffset);
				}
			}
		}
		#endregion

		#region PixelPerfect
		[Tooltip("Controls own canvas Pixel Perfect property.")]
		[SerializeField]
		private bool pixelPerfect = true;

		/// <summary>
		/// Controls own canvas Pixel Perfect property.
		/// </summary>
		public bool PixelPerfect
		{
			get { return pixelPerfect; }
			set
			{
				if (pixelPerfect == value || !Application.isPlaying) return;
				pixelPerfect = value;

				if (operationMode == OperationMode.Disabled || labels == null) return;

				canvas.pixelPerfect = pixelPerfect;
			}
		}
		#endregion

		/* advanced settings */

		#region SortingOrder
		[Tooltip("Sorting order to use for the canvas.\nSet higher value to get closer to the user.")]
		[SerializeField]
		private int sortingOrder = 10000;

		/// <summary>
		/// Sorting order to use for the canvas.
		/// </summary>
		/// Set higher value to get closer to the user.
		public int SortingOrder
		{
			get { return sortingOrder; }
			set
			{
				if (sortingOrder == value || !Application.isPlaying) return;
				sortingOrder = value;

				if (operationMode == OperationMode.Disabled || canvas == null) return;

				canvas.sortingOrder = sortingOrder;
			}
		}
		#endregion

		// preventing direct instantiation
		private AFPSCounter() { }

		// ----------------------------------------------------------------------------
		// instance
		// ----------------------------------------------------------------------------

		/// <summary>
		/// Allows reaching public properties from code. Can be null.
		/// \sa AddToScene()
		/// </summary>
		public static AFPSCounter Instance { get; private set; }

		private static AFPSCounter GetOrCreateInstance(bool keepAlive)
		{
			if (Instance != null) return Instance;

			var counter = FindObjectOfType<AFPSCounter>();
			if (counter != null)
			{
				Instance = counter;
			}
			else
			{
				var newInstance = CreateInScene(false);
				newInstance.keepAlive = keepAlive;
			}
			return Instance;
		}

		// ----------------------------------------------------------------------------
		// public static methods
		// ----------------------------------------------------------------------------

		/// <summary>
		/// Creates and adds new %AFPSCounter instance to the scene if it doesn't exists with keepAlive set to true.
		/// Use it to instantiate %AFPSCounter from code before using AFPSCounter.Instance.
		/// </summary>
		/// <returns>Existing or new %AFPSCounter instance.</returns>
		public static AFPSCounter AddToScene()
		{
			return AddToScene(true);
		}

		/// <summary>
		/// Creates and adds new %AFPSCounter instance to the scene if it doesn't exists.
		/// Use it to instantiate %AFPSCounter from code before using AFPSCounter.Instance.
		/// </summary>
		/// <param name="keepAlive">Set true to prevent AFPSCounter's Game Object from destroying on level (scene) load.
		/// Applies to the new Instance only.</param>
		/// <returns>Existing or new %AFPSCounter instance.</returns>
		public static AFPSCounter AddToScene(bool keepAlive)
		{
			return GetOrCreateInstance(keepAlive);
		}

		[System.Obsolete("Please use SelfDestroy() instead. This method will be removed in future updates.")]
		public static void Dispose()
		{
			SelfDestroy();
		}
		
		/// <summary>
		/// Use it to completely dispose current %AFPSCounter instance.
		/// </summary>
		public static void SelfDestroy()
		{
			if (Instance != null) Instance.DisposeInternal();
		}



		// ----------------------------------------------------------------------------
		// internal static methods
		// ----------------------------------------------------------------------------

		internal static string Color32ToHex(Color32 color)
		{
			return color.r.ToString("x2") + color.g.ToString("x2") + color.b.ToString("x2") + color.a.ToString("x2");
		}

		// ----------------------------------------------------------------------------
		// private static methods
		// ----------------------------------------------------------------------------

		private static AFPSCounter CreateInScene(bool lookForExistingContainer = true)
		{
			var container = lookForExistingContainer ? GameObject.Find(ComponentName) : null;
			if (container == null)
			{
				container = new GameObject(ComponentName)
				{
					layer = LayerMask.NameToLayer("UI")
				};
#if UNITY_EDITOR
				if (!Application.isPlaying)
				{
					UnityEditor.Undo.RegisterCreatedObjectUndo(container, "Create " + ComponentName);
					if (UnityEditor.Selection.activeTransform != null)
					{
						container.transform.parent = UnityEditor.Selection.activeTransform;
					}
					UnityEditor.Selection.activeObject = container;
				}
#endif
			}

			var newInstance = container.AddComponent<AFPSCounter>();
			return newInstance;
		}

		// ----------------------------------------------------------------------------
		// unity callbacks
		// ----------------------------------------------------------------------------

		#region unity callbacks
		private void Awake()
		{
			/* checks for duplication */

			if (Instance != null && Instance.keepAlive)
			{
				Destroy(this);
				return;
			}

			/* editor-only checks */

#if UNITY_EDITOR
			if (!IsPlacedCorrectly())
			{
				Debug.LogWarning(LogPrefix + "incorrect placement detected! Please, use \"" + GameObjectMenuGroup + MenuPath + "\" menu to fix it!", this);
			}
#endif

			/* initialization */

			Instance = this;

			fpsCounter.Init(this);
			memoryCounter.Init(this);
			deviceInfoCounter.Init(this);

			ConfigureCanvas();
			ConfigureLabels();

			inited = true;
		}

		private void Start()
		{
			if (keepAlive)
			{
				// will keep alive itself or other topmost game object
				DontDestroyOnLoad(transform.root.gameObject);
#if UNITY_5_4_OR_NEWER
				SceneManager.sceneLoaded += OnLevelWasLoadedNew;
#endif
			}
		}

		private void Update()
		{
			if (!inited) return;

			ProcessHotKey();

			if (circleGesture && CircleGestureMade())
			{
				SwitchCounter();
			}
		}

#if UNITY_5_4_OR_NEWER
		private void OnLevelWasLoadedNew(Scene scene, LoadSceneMode mode)
		{
			OnLevelLoadedCallback();
		}
#else
		private void OnLevelWasLoaded()
		{
			OnLevelLoadedCallback();
		}
#endif

		private void OnLevelLoadedCallback()
		{
			if (!inited) return;
			if (!fpsCounter.Enabled) return;

			fpsCounter.OnLevelLoadedCallback();
		}

		private void OnEnable()
		{
			if (!inited) return;
			if (operationMode == OperationMode.Disabled) return;

			ActivateCounters();
			Invoke("RefreshForcedFrameRate", 0.5f);
		}

		private void OnDisable()
		{
			if (!inited) return;

			DeactivateCounters();
			if (IsInvoking("RefreshForcedFrameRate")) CancelInvoke("RefreshForcedFrameRate");
			RefreshForcedFrameRate(true);

			for (var i = 0; i < anchorsCount; i++)
			{
				labels[i].Clear();
			}
		}

		private void OnDestroy()
		{
			if (inited)
			{
				fpsCounter.Destroy();
				memoryCounter.Destroy();
				deviceInfoCounter.Destroy();

				if (labels != null)
				{
					for (var i = 0; i < anchorsCount; i++)
					{
						labels[i].Destroy();
					}

					System.Array.Clear(labels, 0, anchorsCount);
					labels = null;
				}
				inited = false;
			}

			if (canvas != null)
			{
				Destroy(canvas.gameObject);
			}

			if (transform.childCount <= 1)
			{
				Destroy(gameObject);
			}

			if (Instance == this) Instance = null;
		}

		#endregion

		// ----------------------------------------------------------------------------
		// internal methods
		// ----------------------------------------------------------------------------

		internal void MakeDrawableLabelDirty(LabelAnchor anchor)
		{
			if (operationMode == OperationMode.Normal)
			{
				labels[(int)anchor].dirty = true;
			}
		}

		internal void UpdateTexts()
		{
			if (operationMode != OperationMode.Normal) return;

			var anyContentPresent = false;

			if (fpsCounter.Enabled)
			{
				var label = labels[(int)fpsCounter.Anchor];
				if (label.newText.Length > 0)
				{
					label.newText.Append(new string(NewLine, countersSpacing + 1));
				}
				label.newText.Append(fpsCounter.text);
				label.dirty |= fpsCounter.dirty;
				fpsCounter.dirty = false;

				anyContentPresent = true;
			}

			if (memoryCounter.Enabled)
			{
				var label = labels[(int)memoryCounter.Anchor];
				if (label.newText.Length > 0)
				{
					label.newText.Append(new string(NewLine, countersSpacing + 1));
				}
				label.newText.Append(memoryCounter.text);
				label.dirty |= memoryCounter.dirty;
				memoryCounter.dirty = false;

				anyContentPresent = true;
			}

			if (deviceInfoCounter.Enabled)
			{
				var label = labels[(int)deviceInfoCounter.Anchor];
				if (label.newText.Length > 0)
				{
					label.newText.Append(new string(NewLine, countersSpacing + 1));
				}
				label.newText.Append(deviceInfoCounter.text);
				label.dirty |= deviceInfoCounter.dirty;
				deviceInfoCounter.dirty = false;

				anyContentPresent = true;
			}

			if (anyContentPresent)
			{
				for (var i = 0; i < anchorsCount; i++)
				{
					labels[i].CheckAndUpdate();
				}
			}
			else
			{
				for (var i = 0; i < anchorsCount; i++)
				{
					labels[i].Clear();
				}
			}
		}

		// ----------------------------------------------------------------------------
		// private methods
		// ----------------------------------------------------------------------------
		private void ConfigureCanvas()
		{
			var parentCanvas = GetComponentInParent<Canvas>();
			if (parentCanvas != null)
			{
				externalCanvas = true;

				var ownRectTransform = gameObject.GetComponent<RectTransform>();
				if (ownRectTransform == null)
				{
					ownRectTransform = gameObject.AddComponent<RectTransform>();
				}

				UIUtils.ResetRectTransform(ownRectTransform);
			}

			var canvasObject = new GameObject("CountersCanvas", typeof(Canvas));
			canvasObject.tag = gameObject.tag;
			canvasObject.layer = gameObject.layer;
			canvasObject.transform.SetParent(transform, false);

			canvas = canvasObject.GetComponent<Canvas>();

			var canvasRectTransform = canvasObject.GetComponent<RectTransform>();

			UIUtils.ResetRectTransform(canvasRectTransform);

			if (!externalCanvas)
			{
				canvas.renderMode = RenderMode.ScreenSpaceOverlay;
				canvas.pixelPerfect = pixelPerfect;
				canvas.sortingOrder = sortingOrder;

				canvasScaler = canvasObject.AddComponent<CanvasScaler>();

				if (autoScale)
				{
					canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
				}
				else
				{
					canvasScaler.scaleFactor = scaleFactor;
				}
			}
		}

		private void ConfigureLabels()
		{
			anchorsCount = System.Enum.GetNames(typeof(LabelAnchor)).Length;
			labels = new DrawableLabel[anchorsCount];

			for (var i = 0; i < anchorsCount; i++)
			{
				labels[i] = new DrawableLabel(canvas.gameObject, (LabelAnchor)i,
					new LabelEffect(background, backgroundColor, backgroundPadding),
					new LabelEffect(shadow, shadowColor, shadowDistance),
					new LabelEffect(outline, outlineColor, outlineDistance),
					labelsFont, fontSize, lineSpacing, paddingOffset);
			}
		}

		private void DisposeInternal()
		{
			Destroy(this);
			if (Instance == this) Instance = null;
		}

		private void ProcessHotKey()
		{
			if (hotKey == KeyCode.None) return;

			if (Input.GetKeyDown(hotKey))
			{
				var modifiersPressed = true;

				if (hotKeyCtrl)
				{
					modifiersPressed &= Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
				}

				if (hotKeyAlt)
				{
					modifiersPressed &= Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
				}

				if (hotKeyShift)
				{
					modifiersPressed &= Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
				}

				if (modifiersPressed)
				{
					SwitchCounter();
				}
			}
		}

		private bool CircleGestureMade()
		{
			var pointsCount = gesturePoints.Count;

			if (Input.GetMouseButton(0))
			{
				Vector2 mousePosition = Input.mousePosition;
				if (pointsCount == 0 || (mousePosition - gesturePoints[pointsCount - 1]).magnitude > 10)
				{
					gesturePoints.Add(mousePosition);
					pointsCount++;
				}
			}
			else if (Input.GetMouseButtonUp(0))
			{
				pointsCount = 0;
				gestureCount = 0;
				gesturePoints.Clear();
			}

			if (pointsCount < 10)
				return false;

			float finalDeltaLength = 0;

			var finalDelta = Vector2.zero;
			var previousPointsDelta = Vector2.zero;

			for (var i = 0; i < pointsCount - 2; i++)
			{
				var pointsDelta = gesturePoints[i + 1] - gesturePoints[i];
				finalDelta += pointsDelta;

				var pointsDeltaLength = pointsDelta.magnitude;
				finalDeltaLength += pointsDeltaLength;

				var dotProduct = Vector2.Dot(pointsDelta, previousPointsDelta);
				if (dotProduct < 0f)
				{
					gesturePoints.Clear();
					gestureCount = 0;
					return false;
				}

				previousPointsDelta = pointsDelta;
			}

			var result = false;
			var gestureBase = (Screen.width + Screen.height) / 4;

			if (finalDeltaLength > gestureBase && finalDelta.magnitude < gestureBase / 2f)
			{
				gesturePoints.Clear();
				gestureCount++;

				if (gestureCount >= 2)
				{
					gestureCount = 0;
					result = true;
				}
			}

			return result;
		}

		private void SwitchCounter()
		{
			if (operationMode == OperationMode.Disabled)
			{
				OperationMode = OperationMode.Normal;
			}
			else if (operationMode == OperationMode.Normal)
			{
				OperationMode = OperationMode.Disabled;
			}
		}

		private void ActivateCounters()
		{
			fpsCounter.Activate();
			memoryCounter.Activate();
			deviceInfoCounter.Activate();

			if (fpsCounter.Enabled || memoryCounter.Enabled || deviceInfoCounter.Enabled)
			{
				UpdateTexts();
			}
		}

		private void DeactivateCounters()
		{
			if (Instance == null) return;

			fpsCounter.Deactivate();
			memoryCounter.Deactivate();
			deviceInfoCounter.Deactivate();
		}

		private void RefreshForcedFrameRate()
		{
			RefreshForcedFrameRate(false);
		}

		private void RefreshForcedFrameRate(bool disabling)
		{
			if (forceFrameRate && !disabling)
			{
				if (cachedVSync == -1)
				{
					cachedVSync = QualitySettings.vSyncCount;
					cachedFrameRate = Application.targetFrameRate;
					QualitySettings.vSyncCount = 0;
				}

				Application.targetFrameRate = forcedFrameRate;
			}
			else
			{
				if (cachedVSync != -1)
				{
					QualitySettings.vSyncCount = cachedVSync;
					Application.targetFrameRate = cachedFrameRate;
					cachedVSync = -1;
				}
			}
		}

		// ----------------------------------------------------------------------------
		// editor-only code
		// ----------------------------------------------------------------------------

		#region editor-only code
#if UNITY_EDITOR
		private const string GameObjectMenuGroup = "GameObject/Create Other/";
		private const string GameObjectMenuPath = GameObjectMenuGroup + MenuPath;

		[HideInInspector]
		[SerializeField]
		private bool lookAndFeelFoldout;

		[HideInInspector]
		[SerializeField]
		private bool advancedFoldout;

		// ReSharper disable once UnusedMember.Local
		[UnityEditor.MenuItem(GameObjectMenuPath, false)]
		private static void AddToSceneInEditor()
		{
			var counter = FindObjectOfType<AFPSCounter>();
			if (counter != null)
			{
				if (counter.IsPlacedCorrectly())
				{
					if (UnityEditor.EditorUtility.DisplayDialog("Remove " + ComponentName + "?",
						ComponentName + " already exists in scene and placed correctly. Do you wish to remove it?", "Yes", "No"))
					{
						DestroyInEditorImmediate(counter);
					}
				}
				else
				{
					if (counter.MayBePlacedHere())
					{
						var dialogResult = UnityEditor.EditorUtility.DisplayDialogComplex("Fix existing Game Object to work with " +
							ComponentName + "?",
							ComponentName + " already exists in scene and placed on Game Object \"" +
							counter.name + "\" with minor errors.\nDo you wish to let plugin fix and use this Game Object further? " +
							"Press Delete to remove plugin from scene at all.", "Fix", "Delete", "Cancel");

						switch (dialogResult)
						{
							case 0:
								counter.FixCurrentGameObject();
								break;
							case 1:
								DestroyInEditorImmediate(counter);
								break;
						}
					}
					else
					{
						var dialogResult = UnityEditor.EditorUtility.DisplayDialogComplex("Move existing " + ComponentName +
							" to own Game Object?",
							"Looks like " + ComponentName + " already exists in scene and placed incorrectly on Game Object \"" +
							counter.name + "\".\nDo you wish to let plugin move itself onto separate correct Game Object \"" +
							ComponentName + "\"? Press Delete to remove plugin from scene at all.", "Move", "Delete", "Cancel");

						switch (dialogResult)
						{
							case 0:
								var newCounter = CreateInScene(false);
								UnityEditor.EditorUtility.CopySerialized(counter, newCounter);
								DestroyInEditorImmediate(counter);
								break;
							case 1:
								DestroyInEditorImmediate(counter);
								break;
						}
					}
				}
			}
			else
			{
				var newCounter = CreateInScene();

				var fonts = UnityEditor.AssetDatabase.FindAssets("t:Font, VeraMono");
				if (fonts != null && fonts.Length != 0)
				{
					string veraMonoPath = null;
					foreach (var font in fonts)
					{
						var fontPath = UnityEditor.AssetDatabase.GUIDToAssetPath(font);
						var fontFileName = System.IO.Path.GetFileName(fontPath);
						if (fontFileName == "VeraMono.ttf")
						{
							veraMonoPath = fontPath;
							break;
						}
					}

					if (!string.IsNullOrEmpty(veraMonoPath))
					{
						var font = (Font) UnityEditor.AssetDatabase.LoadAssetAtPath(veraMonoPath, typeof(Font));
						var so = new UnityEditor.SerializedObject(newCounter);
						so.FindProperty("labelsFont").objectReferenceValue = font;
						so.ApplyModifiedProperties();
						UnityEditor.EditorUtility.SetDirty(newCounter);
					}
				}
				UnityEditor.EditorUtility.DisplayDialog("Advanced FPS Counter added!", "Advanced FPS Counter successfully added to the object \"" + ComponentName + "\"", "OK");
			}

		}

		private bool MayBePlacedHere()
		{
			return transform.childCount == 0;
		}

		private bool IsPlacedCorrectly()
		{
			return transform.localRotation == Quaternion.identity &&
				transform.localScale == Vector3.one &&
				gameObject.isStatic == false &&
				MayBePlacedHere();
		}

		private void FixCurrentGameObject()
		{
			gameObject.name = ComponentName;
			transform.position = Vector3.zero;
			transform.rotation = Quaternion.identity;
			transform.localScale = Vector3.one;
			tag = "Untagged";
			gameObject.layer = LayerMask.NameToLayer("UI");
			gameObject.isStatic = false;
		}

		private static void DestroyInEditorImmediate(AFPSCounter component)
		{
			if (component.transform.childCount == 0 && component.GetComponentsInChildren<Component>(true).Length <= 2)
			{
				UnityEditor.Undo.DestroyObjectImmediate(component.gameObject);
			}
			else
			{
				UnityEditor.Undo.DestroyObjectImmediate(component);
			}
		}
#endif
		#endregion
	}
}