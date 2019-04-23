namespace CodeStage.AdvancedFPSCounter.CountersData
{
	using System;
	using System.Collections;

	using Utils;

	using UnityEngine;
	using Object = UnityEngine.Object;

	/// <summary>
	/// Shows frames per second counter.
	/// </summary>
	[AddComponentMenu("")]
	[Serializable]
	public class FPSCounterData: UpdatableCounterData
	{
		// ----------------------------------------------------------------------------
		// constants
		// ----------------------------------------------------------------------------

		private const string ColorTextStart = "<color=#{0}>";
		private const string ColorTextEnd = "</color>";
		private const string FPSTextStart = ColorTextStart + "FPS: ";
		private const string MsTextStart = " " + ColorTextStart + "[";
		private const string MsTextEnd = " MS]" + ColorTextEnd;
		private const string MinTextStart = ColorTextStart + "MIN: ";
		private const string MaxTextStart = ColorTextStart + "MAX: ";
		private const string AvgTextStart = ColorTextStart + "AVG: ";
		private const string RenderTextStart = ColorTextStart + "REN: ";

		// ----------------------------------------------------------------------------
		// public fields exposed to the inspector
		// ----------------------------------------------------------------------------

		/// <summary>
		/// If FPS will drop below this value, #ColorWarning will be used for counter text.
		/// </summary>
		public int warningLevelValue = 50;

		/// <summary>
		/// If FPS will be equal or less this value, #ColorCritical will be used for counter text.
		/// </summary>
		public int criticalLevelValue = 20;

		/// <summary>
		/// Average FPS counter accumulative data will be reset on new scene load if enabled.
		/// \sa AverageSamples, LastAverageValue
		/// </summary>
		[Tooltip("Average FPS counter accumulative data will be reset on new scene load if enabled.")]
		public bool resetAverageOnNewScene;

		/// <summary>
		/// Minimum and maximum FPS readings will be reset on new scene load if enabled.
		/// \sa LastMinimumValue, LastMaximumValue
		/// </summary>
		[Tooltip("Minimum and maximum FPS readouts will be reset on new scene load if enabled")]
		public bool resetMinMaxOnNewScene;

		/// <summary>
		/// Amount of update intervals to skip before recording minimum and maximum FPS. Use it to skip initialization performance spikes and drops.
		/// \sa LastMinimumValue, LastMaximumValue
		/// </summary>
		[Tooltip("Amount of update intervals to skip before recording minimum and maximum FPS.\n" +
		         "Use it to skip initialization performance spikes and drops.")]
		[Range(0, 10)]
		public int minMaxIntervalsToSkip = 3;

		// ----------------------------------------------------------------------------
		// events
		// ----------------------------------------------------------------------------

		/// <summary>
		/// Event to let you react on FPS level change.
		/// \sa FPSLevel, CurrentFpsLevel
		/// </summary>
		public event Action<FPSLevel> OnFPSLevelChange;

		// ----------------------------------------------------------------------------
		// internal fields
		// ----------------------------------------------------------------------------

		internal float newValue;

		// ----------------------------------------------------------------------------
		// private fields
		// ----------------------------------------------------------------------------

		private string colorCachedMs;
		private string colorCachedMin;
		private string colorCachedMax;
		private string colorCachedAvg;
		private string colorCachedRender;

		private string colorWarningCached;
		private string colorWarningCachedMs;
		private string colorWarningCachedMin;
		private string colorWarningCachedMax;
		private string colorWarningCachedAvg;

		private string colorCriticalCached;
		private string colorCriticalCachedMs;
		private string colorCriticalCachedMin;
		private string colorCriticalCachedMax;
		private string colorCriticalCachedAvg;

		private int currentAverageSamples;
		private float currentAverageRaw;
		private float[] accumulatedAverageSamples;

		private int minMaxIntervalsSkipped;

		private float renderTimeBank;
		private int previousFrameCount;

		// ----------------------------------------------------------------------------
		// properties exposed to the inspector
		// ----------------------------------------------------------------------------

		#region Milliseconds
		[Tooltip("Shows time in milliseconds spent to render 1 frame.")]
		[SerializeField]
		private bool milliseconds;

		/// <summary>
		/// Shows time in milliseconds spent to render 1 frame.
		/// \sa LastMillisecondsValue
		/// </summary>
		public bool Milliseconds
		{
			get { return milliseconds; }
			set
			{
				if (milliseconds == value || !Application.isPlaying) return;
				milliseconds = value;
				if (!milliseconds) LastMillisecondsValue = 0f;
				if (!enabled) return;

				Refresh();
			}
		}
		#endregion

		#region Average
		[Tooltip("Shows Average FPS calculated from specified Samples amount or since game / scene start, " +
		         "depending on Samples value and 'Reset On Load' toggle.")]
		[SerializeField]
		private bool average;

		/// <summary>
		/// Shows Average FPS calculated from specified #AverageSamples amount or since game / scene start, depending on #AverageSamples value and #resetAverageOnNewScene toggle.
		/// \sa LastAverageValue
		/// </summary>
		public bool Average
		{
			get { return average; }
			set
			{
				if (average == value || !Application.isPlaying) return;
				average = value;
				if (!average) ResetAverage();
				if (!enabled) return;

				Refresh();
			}
		}
		#endregion

		#region AverageMilliseconds
		[Tooltip("Shows time in milliseconds for the average FPS.")]
		[SerializeField]
		private bool averageMilliseconds;

		/// <summary>
		/// Shows time in milliseconds for the average FPS.
		/// </summary>
		public bool AverageMilliseconds
		{
			get { return averageMilliseconds; }
			set
			{
				if (averageMilliseconds == value || !Application.isPlaying) return;
				averageMilliseconds = value;
				if (!averageMilliseconds) LastAverageMillisecondsValue = 0f;
				if (!enabled) return;

				Refresh();
			}
		}
		#endregion

		#region AverageNewLine
		[Tooltip("Controls placing Average on the new line.")]
		[SerializeField]
		private bool averageNewLine;

		/// <summary>
		/// Controls placing Average on the new line.
		/// \sa Average
		/// </summary>
		public bool AverageNewLine
		{
			get { return averageNewLine; }
			set
			{
				if (averageNewLine == value || !Application.isPlaying) return;
				averageNewLine = value;
				if (!enabled) return;

				Refresh();
			}
		}
		#endregion

		#region AverageSamples
		[Tooltip("Amount of last samples to get average from. Set 0 to get average from all samples since startup or level load.\n" +
		         "One Sample recorded per one Interval.")]
		[Range(0, 100)]
		[SerializeField]
		private int averageSamples = 50;

		/// <summary>
		/// Amount of last samples to get average from. Set 0 to get average from all samples since startup or level load. One Sample recorded per one #UpdateInterval.
		/// \sa resetAverageOnNewScene
		/// </summary>
		public int AverageSamples
		{
			get { return averageSamples; }
			set
			{
				if (averageSamples == value || !Application.isPlaying) return;
				averageSamples = value;
				if (!enabled) return;

				if (averageSamples > 0)
				{
					if (accumulatedAverageSamples == null)
					{
						accumulatedAverageSamples = new float[averageSamples];
					}
					else if (accumulatedAverageSamples.Length != averageSamples)
					{
						Array.Resize(ref accumulatedAverageSamples, averageSamples);
					}
				}
				else
				{
					accumulatedAverageSamples = null;
				}
				ResetAverage();
				Refresh();
			}
		}
		#endregion

		#region MinMax
		[Tooltip("Shows minimum and maximum FPS readouts since game / scene start, depending on 'Reset On Load' toggle.")]
		[SerializeField]
		private bool minMax;

		/// <summary>
		/// Shows minimum and maximum FPS readouts since game / scene start, depending on #resetMinMaxOnNewScene toggle.
		/// </summary>
		public bool MinMax
		{
			get { return minMax; }
			set
			{
				if (minMax == value || !Application.isPlaying) return;
				minMax = value;
				if (!minMax) ResetMinMax();
				if (!enabled) return;

				Refresh();
			}
		}
		#endregion

		#region MinMaxMilliseconds
		[Tooltip("Shows time in milliseconds for the Min Max FPS.")]
		[SerializeField]
		private bool minMaxMilliseconds;

		/// <summary>
		/// Shows time in milliseconds for the Min Max FPS.
		/// </summary>
		public bool MinMaxMilliseconds
		{
			get { return minMaxMilliseconds; }
			set
			{
				if (minMaxMilliseconds == value || !Application.isPlaying) return;
				minMaxMilliseconds = value;
				if (!minMaxMilliseconds)
				{
					LastMinMillisecondsValue = 0f;
					LastMaxMillisecondsValue = 0f;
				}
				else
				{
					LastMinMillisecondsValue = 1000f / LastMinimumValue;
					LastMaxMillisecondsValue = 1000f / LastMaximumValue;
				}
				if (!enabled) return;

				Refresh();
			}
		}
		#endregion

		#region MinMaxNewLine
		[Tooltip("Controls placing Min Max on the new line.")]
		[SerializeField]
		private bool minMaxNewLine;

		/// <summary>
		/// Controls placing Min Max on the new line.
		/// \sa MinMax
		/// </summary>
		public bool MinMaxNewLine
		{
			get { return minMaxNewLine; }
			set
			{
				if (minMaxNewLine == value || !Application.isPlaying) return;
				minMaxNewLine = value;
				if (!enabled) return;

				Refresh();
			}
		}
		#endregion

		#region MinMaxTwoLines
        [Tooltip("Check to place Min Max on two separate lines. Otherwise they will be placed on a single line.")]
        [SerializeField]
        private bool minMaxTwoLines;

        /// <summary>
        /// Check to place Min Max on two separate lines. Otherwise they will be placed on a single line.
        /// \sa MinMax
        /// </summary>
        public bool MinMaxTwoLines
        {
            get { return minMaxTwoLines; }
            set
            {
                if (minMaxTwoLines == value || !Application.isPlaying) return;
                minMaxTwoLines = value;
                if (!enabled) return;

                Refresh();
            }
        }
		#endregion

		#region Render
		[Tooltip("Shows time spent on Camera.Render excluding Image Effects. Add AFPSRenderRecorder to the cameras you wish to count.")]
		[SerializeField]
		private bool render;

		/// <summary>
		/// Shows approximate time in ms spent on Camera.Render excluding Image Effects and IMGUI. 
		/// Requires \link CodeStage.AdvancedFPSCounter.Utils.AFPSRenderRecorder AFPSRenderRecorder \endlink added to the cameras you wish to count.
		/// </summary>
		/// <strong>\htmlonly<font color="7030A0">NOTE:</font>\endhtmlonly It doesn't take into account Image Effects and IMGUI!</strong>
		/// \sa LastRenderValue
		/// \sa \link CodeStage.AdvancedFPSCounter.Utils.AFPSRenderRecorder AFPSRenderRecorder \endlink
		public bool Render
		{
			get { return render; }
			set
			{
				if (render == value || !Application.isPlaying) return;
				render = value;

				if (!render)
				{
					if (renderAutoAdd) TryToRemoveRenderRecorder();
					return;
				}

				previousFrameCount = Time.frameCount;
				if (renderAutoAdd) TryToAddRenderRecorder();

				Refresh();
			}
		}
		#endregion

		#region RenderNewLine
		[Tooltip("Controls placing Render on the new line.")]
		[SerializeField]
		private bool renderNewLine;

		/// <summary>
		/// Controls placing Render on the new line.
		/// \sa Render
		/// </summary>
		public bool RenderNewLine
		{
			get { return renderNewLine; }
			set
			{
				if (renderNewLine == value || !Application.isPlaying) return;
				renderNewLine = value;
				if (!enabled) return;

				Refresh();
			}
		}
		#endregion

		#region RenderAutoAdd
		[Tooltip("Check to automatically add AFPSRenderRecorder to the Main Camera if present.")]
		[SerializeField]
		private bool renderAutoAdd = true;

		/// <summary>
		/// Check to let FPS Counter try automatically add AFPSRenderRecorder to the Camera with MainCamera tag.
		/// <br/>You're free to add AFPSRenderRecorder to any cameras you wish to count.
		/// \sa Render
		/// </summary>
		public bool RenderAutoAdd
		{
			get { return renderAutoAdd; }
			set
			{
				if (renderAutoAdd == value || !Application.isPlaying) return;
				renderAutoAdd = value;
				if (!enabled) return;

				TryToAddRenderRecorder();

				Refresh();
			}
		}
		#endregion

		#region ColorWarning
		[Tooltip("Color of the FPS counter while FPS is between Critical and Warning levels.")]
		[SerializeField]
		private Color colorWarning = new Color32(236, 224, 88, 255);

		/// <summary>
		/// Color of the FPS counter while FPS is between #criticalLevelValue and #warningLevelValue levels.
		/// </summary>
		public Color ColorWarning
		{
			get { return colorWarning; }
			set
			{
				if (colorWarning == value || !Application.isPlaying) return;
				colorWarning = value;
				if (!enabled) return;

				CacheWarningColor();

				Refresh();
			}
		}
		#endregion

		#region ColorCritical
		[Tooltip("Color of the FPS counter while FPS is below Critical level.")]
		[SerializeField]
		private Color colorCritical = new Color32(249, 91, 91, 255);

		/// <summary>
		/// Color of the FPS counter while FPS is below #criticalLevelValue.
		/// </summary>
		public Color ColorCritical
		{
			get { return colorCritical; }
			set
			{
				if (colorCritical == value || !Application.isPlaying) return;
				colorCritical = value;
				if (!enabled) return;

				CacheCriticalColor();

				Refresh();
			}
		}
		#endregion

		#region ColorRender
		[Tooltip("Color of the Render Time output.")]
		[SerializeField]
		protected Color colorRender;

		/// <summary>
		/// Color of the Render Time output.
		/// </summary>
		public Color ColorRender
		{
			get { return colorRender; }
			set
			{
				if (colorRender == value || !Application.isPlaying) return;
				colorRender = value;
				if (!enabled) return;

				CacheCurrentColor();

				Refresh();
			}
		}
		#endregion

		// ----------------------------------------------------------------------------
		// properties only accessible from code
		// ----------------------------------------------------------------------------

		/// <summary>
		/// Last calculated FPS value.
		/// </summary>
		public int LastValue { get; private set; }

		/// <summary>
		/// Last calculated Milliseconds value.
		/// </summary>
		public float LastMillisecondsValue { get; private set; }

		/// <summary>
		/// Last calculated Render Time value.
		/// \sa Render
		/// </summary>
		public float LastRenderValue { get; private set; }

		/// <summary>
		/// Last calculated Average FPS value.
		/// \sa AverageSamples, resetAverageOnNewScene
		/// </summary>
		public int LastAverageValue { get; private set; }
		
		/// <summary>
		/// Last calculated Milliseconds value for Average FPS.
		/// </summary>
		public float LastAverageMillisecondsValue { get; private set; }

		/// <summary>
		/// Last minimum FPS value.
		/// \sa resetMinMaxOnNewScene
		/// </summary>
		public int LastMinimumValue { get; private set; }

		/// <summary>
		/// Last maximum FPS value.
		/// \sa resetMinMaxOnNewScene
		/// </summary>
		public int LastMaximumValue { get; private set; }

		/// <summary>
		/// Last calculated Milliseconds value for Minimum FPS.
		/// \sa resetMinMaxOnNewScene
		/// </summary>
		public float LastMinMillisecondsValue { get; private set; }

		/// <summary>
		/// Last calculated Milliseconds value for Maximum FPS.
		/// \sa resetMinMaxOnNewScene
		/// </summary>
		public float LastMaxMillisecondsValue { get; private set; }

		/// <summary>
		/// Current FPS level.
		/// \sa FPSLevel, OnFPSLevelChange
		/// </summary>
		public FPSLevel CurrentFpsLevel { get; private set; }

		// ----------------------------------------------------------------------------
		// constructor
		// ----------------------------------------------------------------------------

		internal FPSCounterData()
		{
			color = new Color32(85, 218, 102, 255);
			colorRender = new Color32(167, 110, 209, 255);
			style = FontStyle.Bold;
			milliseconds = true;
			render = false;
			renderNewLine = true;
			average = true;
			averageMilliseconds = true;
			averageNewLine = true;
			resetAverageOnNewScene = true;
			minMax = true;
			minMaxNewLine = true;
			resetMinMaxOnNewScene = true;
		}

		// ----------------------------------------------------------------------------
		// public methods
		// ----------------------------------------------------------------------------

		/// <summary>
		/// Resets Average FPS counter accumulative data.
		/// </summary>
		public void ResetAverage()
		{
			if (!Application.isPlaying) return;

			LastAverageValue = 0;
			currentAverageSamples = 0;
			currentAverageRaw = 0;

			if (averageSamples > 0 && accumulatedAverageSamples != null)
			{
				Array.Clear(accumulatedAverageSamples, 0, accumulatedAverageSamples.Length);
			}
		}

		/// <summary>
		/// Resets minimum and maximum FPS readings.
		/// </summary>
		public void ResetMinMax(bool withoutUpdate = false)
		{
			if (!Application.isPlaying) return;
			LastMinimumValue = -1;
			LastMaximumValue = -1;
			minMaxIntervalsSkipped = 0;
			
			UpdateValue(true);
			dirty = true;
		}

		// ----------------------------------------------------------------------------
		// internal methods
		// ----------------------------------------------------------------------------

		internal void OnLevelLoadedCallback()
		{
			if (minMax && resetMinMaxOnNewScene) ResetMinMax();
			if (average && resetAverageOnNewScene) ResetAverage();
			if (render && renderAutoAdd) TryToAddRenderRecorder();
		}

		internal void AddRenderTime(float time)
		{
			if (!enabled || !inited) return;
			renderTimeBank += time;
		}

		internal override void UpdateValue(bool force)
		{
			if (!enabled) return;

			var roundedValue = (int)newValue;
			if (LastValue != roundedValue || force)
			{
				LastValue = roundedValue;
				dirty = true;
			}

			if (LastValue <= criticalLevelValue)
			{
				if (LastValue != 0 && CurrentFpsLevel != FPSLevel.Critical)
				{
					CurrentFpsLevel = FPSLevel.Critical;
					if (OnFPSLevelChange != null) OnFPSLevelChange(CurrentFpsLevel);
				}
			}
			else if (LastValue < warningLevelValue)
			{
				if (LastValue != 0 && CurrentFpsLevel != FPSLevel.Warning)
				{
					CurrentFpsLevel = FPSLevel.Warning;
					if (OnFPSLevelChange != null) OnFPSLevelChange(CurrentFpsLevel);
				}
			}
			else
			{
				if (LastValue != 0 && CurrentFpsLevel != FPSLevel.Normal)
				{
					CurrentFpsLevel = FPSLevel.Normal;
					if (OnFPSLevelChange != null) OnFPSLevelChange(CurrentFpsLevel);
				}
			}

			// since ms calculates from fps we can calculate it when fps changed
			if (dirty && milliseconds)
			{
				LastMillisecondsValue = 1000f / newValue;
			}

			if (render)
			{
				if (renderTimeBank > 0)
				{
					var frameCount = Time.frameCount;
					var framesSinceLastUpdate = frameCount - previousFrameCount;
					if (framesSinceLastUpdate == 0) framesSinceLastUpdate = 1;
					var renderTime = renderTimeBank / framesSinceLastUpdate;

					if (Math.Abs(renderTime - LastRenderValue) > 0.0001f || force)
					{
						LastRenderValue = renderTime;
						dirty = true;
					}

					previousFrameCount = frameCount;
					renderTimeBank = 0;
				}
			}

			var currentAverageRounded = 0;
			if (average)
			{
				if (averageSamples == 0)
				{
					currentAverageSamples++;
					currentAverageRaw += (LastValue - currentAverageRaw) / currentAverageSamples;
				}
				else
				{
					if (accumulatedAverageSamples == null)
					{
						accumulatedAverageSamples = new float[averageSamples];
						ResetAverage();
					}

					accumulatedAverageSamples[currentAverageSamples % averageSamples] = LastValue;
					currentAverageSamples++;

					currentAverageRaw = GetAverageFromAccumulatedSamples();
				}

				currentAverageRounded = Mathf.RoundToInt(currentAverageRaw);

				if (LastAverageValue != currentAverageRounded || force)
				{
					LastAverageValue = currentAverageRounded;
					dirty = true;

					if (averageMilliseconds)
					{
						LastAverageMillisecondsValue = 1000f/LastAverageValue;
					}
				}
			}

			if (minMax)
			{
				if (minMaxIntervalsSkipped <= minMaxIntervalsToSkip)
				{
					if (!force) minMaxIntervalsSkipped++;
				}
				else if (LastMinimumValue == -1)
				{
					dirty = true;
				}

				if (minMaxIntervalsSkipped > minMaxIntervalsToSkip && dirty)
				{
					if (LastMinimumValue == -1)
					{
						LastMinimumValue = LastValue;
						if (minMaxMilliseconds)
						{
							LastMinMillisecondsValue = 1000f / LastMinimumValue;
						}
					}
					else if (LastValue < LastMinimumValue)
					{
						LastMinimumValue = LastValue;
						if (minMaxMilliseconds)
						{
							LastMinMillisecondsValue = 1000f / LastMinimumValue;
						}
					}

					if (LastMaximumValue == -1)
					{
						LastMaximumValue = LastValue;
						if (minMaxMilliseconds)
						{
							LastMaxMillisecondsValue = 1000f / LastMaximumValue;
						}
					}
					else if (LastValue > LastMaximumValue)
					{
						LastMaximumValue = LastValue;
						if (minMaxMilliseconds)
						{
							LastMaxMillisecondsValue = 1000f / LastMaximumValue;
						}
					}
				}
			}

			if (dirty && main.OperationMode == OperationMode.Normal)
			{
				string coloredStartText;

				if (LastValue >= warningLevelValue)
					coloredStartText = colorCached;
				else if (LastValue <= criticalLevelValue)
					coloredStartText = colorCriticalCached;
				else
					coloredStartText = colorWarningCached;

				text.Length = 0;
				text.Append(coloredStartText).AppendLookup(LastValue).Append(ColorTextEnd);

				if (milliseconds)
				{
					if (LastValue >= warningLevelValue)
						coloredStartText = colorCachedMs;
					else if (LastValue <= criticalLevelValue)
						coloredStartText = colorCriticalCachedMs;
					else
						coloredStartText = colorWarningCachedMs;

					text.Append(coloredStartText).AppendLookup(LastMillisecondsValue).Append(MsTextEnd);
				}

				if (average)
				{
					text.Append(averageNewLine ? AFPSCounter.NewLine : AFPSCounter.Space);

					if (currentAverageRounded >= warningLevelValue)
						coloredStartText = colorCachedAvg;
					else if (currentAverageRounded <= criticalLevelValue)
						coloredStartText = colorCriticalCachedAvg;
					else
						coloredStartText = colorWarningCachedAvg;

					text.Append(coloredStartText).AppendLookup(currentAverageRounded);

					if (averageMilliseconds)
					{
						text.Append(" [").AppendLookup(LastAverageMillisecondsValue).Append(" MS]");
					}
					
					text.Append(ColorTextEnd);
				}

				if (minMax)
				{
					text.Append(minMaxNewLine ? AFPSCounter.NewLine : AFPSCounter.Space);

					if (LastMinimumValue >= warningLevelValue)
						coloredStartText = colorCachedMin;
					else if (LastMinimumValue <= criticalLevelValue)
						coloredStartText = colorCriticalCachedMin;
					else
						coloredStartText = colorWarningCachedMin;

					text.Append(coloredStartText).AppendLookup(LastMinimumValue);

					if (minMaxMilliseconds)
					{
						text.Append(" [").AppendLookup(LastMinMillisecondsValue).Append(" MS]");
					}

					text.Append(ColorTextEnd);

                    text.Append(minMaxTwoLines ? AFPSCounter.NewLine : AFPSCounter.Space);

                    if (LastMaximumValue >= warningLevelValue)
						coloredStartText = colorCachedMax;
					else if (LastMaximumValue <= criticalLevelValue)
						coloredStartText = colorCriticalCachedMax;
					else
						coloredStartText = colorWarningCachedMax;

					text.Append(coloredStartText).AppendLookup(LastMaximumValue);

					if (minMaxMilliseconds)
					{
						text.Append(" [").AppendLookup(LastMaxMillisecondsValue).Append(" MS]");
					}

					text.Append(ColorTextEnd);
				}

				if (render)
				{
					text.Append(renderNewLine ? AFPSCounter.NewLine : AFPSCounter.Space).
						Append(colorCachedRender).
						AppendLookup(LastRenderValue).Append(" MS").
						Append(ColorTextEnd);
				}

				ApplyTextStyles();
			}
		}

	    // ----------------------------------------------------------------------------
		// protected methods
		// ----------------------------------------------------------------------------

		protected override void PerformActivationActions()
		{
			base.PerformActivationActions();

			LastValue = 0;
			LastMinimumValue = -1;

			if (render)
			{
				previousFrameCount = Time.frameCount;
				if (renderAutoAdd)
				{
					TryToAddRenderRecorder();
				}
			}

			if (main.OperationMode == OperationMode.Normal)
			{
				if (colorWarningCached == null)
				{
					CacheWarningColor();
				}

				if (colorCriticalCached == null)
				{
					CacheCriticalColor();
				}
				text.Append(colorCriticalCached).Append("0").Append(ColorTextEnd);
				ApplyTextStyles();
				dirty = true;
			}
		}

		protected override void PerformDeActivationActions()
		{
			base.PerformDeActivationActions();

			ResetMinMax(true);
			ResetAverage();
			LastValue = 0;
			CurrentFpsLevel = FPSLevel.Normal;
		}

		protected override IEnumerator UpdateCounter()
		{
			while (true)
			{
				var previousUpdateTime = Time.unscaledTime;
				var previousUpdateFrames = Time.frameCount;

				while (Time.unscaledTime < previousUpdateTime + updateInterval)
				{
					yield return null;
				}

				var timeElapsed = Time.unscaledTime - previousUpdateTime;
				var framesChanged = Time.frameCount - previousUpdateFrames;

				newValue = framesChanged / timeElapsed;
				UpdateValue(false);
				main.UpdateTexts();
			}
		}

		protected override bool HasData()
		{
			return true;
		}

		protected override void CacheCurrentColor()
		{
			var colorString = AFPSCounter.Color32ToHex(color);
			colorCached = string.Format(FPSTextStart, colorString);
			colorCachedMs = string.Format(MsTextStart, colorString);
			colorCachedMin = string.Format(MinTextStart, colorString);
			colorCachedMax = string.Format(MaxTextStart, colorString);
			colorCachedAvg = string.Format(AvgTextStart, colorString);

			var colorRenderString = AFPSCounter.Color32ToHex(colorRender);
			colorCachedRender = string.Format(RenderTextStart, colorRenderString);
		}

		protected void CacheWarningColor()
		{
			var colorString = AFPSCounter.Color32ToHex(colorWarning);
			colorWarningCached = string.Format(FPSTextStart, colorString);
			colorWarningCachedMs = string.Format(MsTextStart, colorString);
			colorWarningCachedMin = string.Format(MinTextStart, colorString);
			colorWarningCachedMax = string.Format(MaxTextStart, colorString);
			colorWarningCachedAvg = string.Format(AvgTextStart, colorString);
		}

		protected void CacheCriticalColor()
		{
			var colorString = AFPSCounter.Color32ToHex(colorCritical);
			colorCriticalCached = string.Format(FPSTextStart, colorString);
			colorCriticalCachedMs = string.Format(MsTextStart, colorString);
			colorCriticalCachedMin = string.Format(MinTextStart, colorString);
			colorCriticalCachedMax = string.Format(MaxTextStart, colorString);
			colorCriticalCachedAvg = string.Format(AvgTextStart, colorString);
		}

		// ----------------------------------------------------------------------------
		// private methods
		// ----------------------------------------------------------------------------

		private float GetAverageFromAccumulatedSamples()
		{
			float averageFps;
			float totalFps = 0;

			for (var i = 0; i < averageSamples; i++)
			{
				totalFps += accumulatedAverageSamples[i];
			}

			if (currentAverageSamples < averageSamples)
			{
				averageFps = totalFps / currentAverageSamples;
			}
			else
			{
				averageFps = totalFps / averageSamples;
			}

			return averageFps;
		}

		private static void TryToAddRenderRecorder()
		{
			var mainCamera = Camera.main;
			if (mainCamera == null) return;

			if (mainCamera.GetComponent<AFPSRenderRecorder>() == null)
			{
				mainCamera.gameObject.AddComponent<AFPSRenderRecorder>();
			}
		}

		private static void TryToRemoveRenderRecorder()
		{
			var mainCamera = Camera.main;
			if (mainCamera == null) return;

			var recorder = mainCamera.GetComponent<AFPSRenderRecorder>();
			if (recorder != null)
			{
				Object.Destroy(recorder);
			}
		}
	}
}