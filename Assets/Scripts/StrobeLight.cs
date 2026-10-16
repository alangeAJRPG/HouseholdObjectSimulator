using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes one or more Unity Lights flash on and off, optionally changing colour
/// for every flash. Attach this component to a light or to a parent of several lights.
/// </summary>
public sealed class StrobeLight : MonoBehaviour
{
    public enum ColourOrder
    {
        InOrder,
        Random
    }

    [Header("Targets")]
    [Tooltip("Lights to strobe. If empty, every Light on this object and its children is used.")]
    [SerializeField] private Light[] lights;
    [Tooltip("Restore each light's original enabled state, colour, and intensity when this component is disabled.")]
    [SerializeField] private bool restoreOriginalValuesOnDisable = true;

    [Header("Playback")]
    [SerializeField] private bool playOnAwake = true;
    [Tooltip("Use real time so the strobe ignores Time.timeScale.")]
    [SerializeField] private bool useUnscaledTime;
    [Min(0f)] [SerializeField] private float startDelay;
    [Min(0.01f)] [SerializeField] private float flashesPerSecond = 8f;
    [Range(0.01f, 0.99f)] [SerializeField] private float onFraction = 0.35f;
    [Tooltip("Randomly varies the duration of every flash by this percentage.")]
    [Range(0f, 1f)] [SerializeField] private float timingJitter;
    [Tooltip("Offsets the first flash, useful when multiple strobes should not sync up.")]
    [Min(0f)] [SerializeField] private float phaseOffset;

    [Header("Brightness")]
    [Tooltip("Intensity used while the strobe is on. Set to 0 to retain each light's original intensity.")]
    [Min(0f)] [SerializeField] private float onIntensity = 1f;
    [Tooltip("Intensity used while the strobe is off. Usually zero; enable 'Keep lights enabled while off' to use it.")]
    [Min(0f)] [SerializeField] private float offIntensity;
    [Tooltip("When enabled, lights remain enabled during the dark part of a flash and use Off Intensity instead.")]
    [SerializeField] private bool keepLightsEnabledWhileOff;

    [Header("Colours")]
    [Tooltip("Enable this to set a colour each time the strobe turns on.")]
    [SerializeField] private bool cycleColours = true;
    [Tooltip("The palette to rotate through. Add as many colours as needed.")]
    [SerializeField] private List<Color> colours = new List<Color> { Color.white, Color.red, Color.blue };
    [Tooltip("How many entries from the start of Colours are included in the rotation. Zero uses the entire list.")]
    [Min(0)] [SerializeField] private int colourCount;
    [SerializeField] private ColourOrder colourOrder = ColourOrder.InOrder;
    [Tooltip("Randomizes the first palette entry when playback begins.")]
    [SerializeField] private bool randomizeStartingColour;

    private struct LightState
    {
        public bool enabled;
        public float intensity;
        public Color colour;
    }

    private readonly List<LightState> originalStates = new List<LightState>();
    private bool isPlaying;
    private bool isOn;
    private float timer;
    private float currentCycleDuration;
    private int nextColourIndex;

    /// <summary>Whether the strobe is currently advancing and controlling its assigned lights.</summary>
    public bool IsPlaying { get { return isPlaying; } }

    private void Awake()
    {
        FindLightsIfNeeded();
        CacheOriginalStates();
    }

    private void OnEnable()
    {
        if (playOnAwake)
            Play();
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        timer -= useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        while (timer <= 0f)
            AdvanceFlash();
    }

    private void OnDisable()
    {
        if (restoreOriginalValuesOnDisable)
            RestoreOriginalStates();
    }

    /// <summary>Starts the strobe. Calling this while it is playing restarts its timing.</summary>
    public void Play()
    {
        FindLightsIfNeeded();
        if (originalStates.Count != lights.Length)
            CacheOriginalStates();

        isPlaying = true;
        isOn = false;
        nextColourIndex = randomizeStartingColour ? RandomColourIndex() : 0;
        timer = Mathf.Max(0f, startDelay + phaseOffset);
        SetLightsOff();
    }

    /// <summary>Stops on a dark frame. Use DisableAndRestore to return the original light settings.</summary>
    public void Stop()
    {
        isPlaying = false;
        isOn = false;
        SetLightsOff();
    }

    /// <summary>Stops the strobe and restores its lights' values from before it began.</summary>
    public void StopAndRestore()
    {
        isPlaying = false;
        isOn = false;
        RestoreOriginalStates();
    }

    public void Toggle()
    {
        if (isPlaying)
            Stop();
        else
            Play();
    }

    /// <summary>Immediately starts one new lit flash.</summary>
    public void FlashNow()
    {
        if (!isPlaying)
            isPlaying = true;

        isOn = false;
        timer = 0f;
        AdvanceFlash();
    }

    private void AdvanceFlash()
    {
        isOn = !isOn;
        if (isOn)
        {
            SetLightsOn();
            currentCycleDuration = CycleDuration();
            timer += currentCycleDuration * onFraction;
        }
        else
        {
            SetLightsOff();
            timer += currentCycleDuration * (1f - onFraction);
        }
    }

    private float CycleDuration()
    {
        float duration = 1f / Mathf.Max(0.01f, flashesPerSecond);
        return duration * Random.Range(1f - timingJitter, 1f + timingJitter);
    }

    private void SetLightsOn()
    {
        Color colour = cycleColours ? NextColour() : Color.white;
        foreach (Light light in lights)
        {
            if (light == null)
                continue;

            light.enabled = true;
            if (onIntensity > 0f)
                light.intensity = onIntensity;
            if (cycleColours && colours != null && colours.Count > 0)
                light.color = colour;
        }
    }

    private void SetLightsOff()
    {
        foreach (Light light in lights)
        {
            if (light == null)
                continue;

            light.enabled = keepLightsEnabledWhileOff;
            if (keepLightsEnabledWhileOff)
                light.intensity = offIntensity;
        }
    }

    private Color NextColour()
    {
        int usableColourCount = UsableColourCount();
        if (usableColourCount == 0)
            return Color.white;

        int index;
        if (colourOrder == ColourOrder.Random)
            index = RandomColourIndex();
        else
        {
            index = nextColourIndex % usableColourCount;
            nextColourIndex = (nextColourIndex + 1) % usableColourCount;
        }
        return colours[index];
    }

    private int RandomColourIndex()
    {
        int usableColourCount = UsableColourCount();
        return usableColourCount == 0 ? 0 : Random.Range(0, usableColourCount);
    }

    private int UsableColourCount()
    {
        if (colours == null)
            return 0;
        return colourCount == 0 ? colours.Count : Mathf.Min(colourCount, colours.Count);
    }

    private void FindLightsIfNeeded()
    {
        if (lights == null || lights.Length == 0)
            lights = GetComponentsInChildren<Light>(true);
    }

    private void CacheOriginalStates()
    {
        originalStates.Clear();
        foreach (Light light in lights)
        {
            originalStates.Add(light == null
                ? new LightState()
                : new LightState { enabled = light.enabled, intensity = light.intensity, colour = light.color });
        }
    }

    private void RestoreOriginalStates()
    {
        int count = Mathf.Min(lights == null ? 0 : lights.Length, originalStates.Count);
        for (int i = 0; i < count; i++)
        {
            Light light = lights[i];
            if (light == null)
                continue;

            LightState state = originalStates[i];
            light.enabled = state.enabled;
            light.intensity = state.intensity;
            light.color = state.colour;
        }
    }
}
