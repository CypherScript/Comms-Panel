using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Video;
using System.Security.Cryptography;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

public class CommsPanelManager : MonoBehaviour
{
    [SerializeField] private EventSystem _eventSystem = null;
    [SerializeField] private Button[] _planetButtons = null;
    [SerializeField] private AudioClip[] _beeps = null;
    [SerializeField] private GameObject _screenDimGO = null;
    [SerializeField] private float _screenTimeout = 30f;
    [SerializeField] private float _dimDuration = 1f;
    [SerializeField] private float _fadeDuration = 1f;
    [SerializeField] private float _backgroundBeepsFrequency = 120f;
    [Range(0, 1)]
    [SerializeField] private float _backgroundBeepsVolume = 0.25f;
    [Range(0, 1)]
    [SerializeField] private float _buttonClickVolume = 1f;
    [Range(0, 1)]
    [SerializeField] private float _dimPronounceness = 0.5f;

    private RenderTexture _renderTexture;
    private Transform _previousParent;
    private Vector2 _resolution = new Vector2(2561, 1601);
    private AudioSource _audioPlayer = null;
    private Image _screenDimImage = null;
    private Color _defaultDimColor = Color.black;
    private float _screenTimeoutTimer = 0;
    private float _backgroundBeepsTimer = 0;
    private bool _isScreenDimmed = false;
    private bool _isVideoPlaying = false;

    private void Awake()
    {
        _audioPlayer = GetComponent<AudioSource>();
        _screenDimImage = _screenDimGO.GetComponentInChildren<Image>();

        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Input.multiTouchEnabled = false;
    }

    private void Start()
    {
        StartCoroutine(ScreenTimeout());
    }

    private void Update()
    {
        if (_isVideoPlaying) return;

        //_backgroundBeepsTimer += Time.deltaTime;

        //if(_backgroundBeepsTimer > _backgroundBeepsFrequency && !_isScreenDimmed)
        //{
        //    _backgroundBeepsTimer = 0;
        //    PlayAudioPlayer(_beeps[0], _backgroundBeepsVolume);
        //}

        if (Input.touchCount > 0 || Input.GetMouseButtonDown(0))
        {
            _screenDimImage.color = _defaultDimColor;
            _screenDimGO.SetActive(false);
            _isScreenDimmed = false;

            foreach (Button btn in _planetButtons)
                btn.interactable = true;
        }
    }

    private void PlayVideoPlayer(VideoPlayer vp, bool isLoop)
    {
        if (vp == null)
        {
            Debug.Log("VIDEO PLAYER IS NULL!");
            return;
        }

        if(vp.clip == null)
        {
            Debug.Log("VIDEO PLAYER CLIP IS NULL!");
            return;
        }

        RawImage rawImage = vp.GetComponentInChildren<RawImage>();
        _renderTexture = new RenderTexture(3840, 2160, (int)RenderTextureFormat.R8);
        _renderTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        vp.targetTexture = _renderTexture;
        rawImage.texture = _renderTexture;
        vp.isLooping = isLoop;
        vp.loopPointReached += StopVideoPlayer;
        vp.frame = 0;
        vp.gameObject.SetActive(true);
        vp.Play();

        StartCoroutine(EnableBackButton());

        IEnumerator EnableBackButton()
        {
            yield return new WaitForSeconds(1);

            GameObject backButton = vp.gameObject.transform.GetChild(1).gameObject;
            backButton.SetActive(true);
            _eventSystem.enabled = true;
            _screenDimImage.color = _defaultDimColor;
            _screenDimGO.SetActive(false);
        }
    }

    private void PlayAudioPlayer(AudioClip audioClip, float volume)
    {
        if (audioClip == null || _audioPlayer == null) return;

        _audioPlayer.Stop();
        _audioPlayer.volume = volume;
        _audioPlayer.clip = audioClip;
        _audioPlayer.Play();
    }

    private void StopVideoPlayer(VideoPlayer vp)
    {
        _eventSystem.enabled = false;
        Image image = vp.gameObject.transform.GetChild(2).GetComponent<Image>();
        image.gameObject.SetActive(true);
        Color newColor = Color.black;
        newColor.a = 0;
        GameObject backButton = vp.gameObject.transform.GetChild(1).gameObject;
        StartCoroutine(DimVideoPlayer());

        IEnumerator DimVideoPlayer()
        {
            float t = 0; 

            while(t < 1)
            {
                t += Time.deltaTime;
                newColor.a = Mathf.Lerp(0, 1, t);
                image.color = newColor;
                _audioPlayer.volume = Mathf.Lerp(_audioPlayer.volume, 0, t);
                yield return null;
            }

            yield return new WaitForSeconds(1);

            backButton.SetActive(false);
            vp.Stop();
            vp.loopPointReached -= StopVideoPlayer;
            vp.gameObject.SetActive(false);
            image.gameObject.SetActive(false);
            _isVideoPlaying = false;
            _backgroundBeepsTimer = 0;

            foreach (Button btn in _planetButtons)
                btn.interactable = true;

            System.GC.Collect();
            Resources.UnloadUnusedAssets();

            _eventSystem.enabled = true;
        }
    }

    public void OnPlanetButtonPressed(VideoPlayer vp)
    {
        _isVideoPlaying = true;
        _eventSystem.enabled = false;

        foreach (Button btn in _planetButtons)
            btn.interactable = false;

        StartCoroutine(FadeScreen(0, 1, vp));
    }

    public void OnPlanetButtonPressed(AudioClip clip)
    {
        PlayAudioPlayer(clip, _buttonClickVolume);
    }

    public void OnBackButtonPressed(VideoPlayer vp)
    {
        StopVideoPlayer(vp);
    }

    private IEnumerator ScreenTimeout()
    {
        while (Input.touchCount < 1 && !_isVideoPlaying && _screenTimeoutTimer < _screenTimeout)
        {
            _screenTimeoutTimer += Time.deltaTime;

            if (_screenTimeoutTimer >= _screenTimeout && !_screenDimGO.activeSelf)
            {
                StartCoroutine(DimScreen());
            }

            yield return null;
        }

        _screenTimeoutTimer = 0f;
        yield return null;
        StartCoroutine(ScreenTimeout());
    }

    private IEnumerator DimScreen()
    {
        _screenDimGO.SetActive(true);

        foreach (Button btn in _planetButtons)
            btn.interactable = false;

        if (_screenDimGO.activeSelf)
        {
            float t = 0;
            Color newColor = Color.black;
            newColor.a = 0;

            while (t < _dimDuration)
            {
                t += Time.deltaTime;
                newColor.a = Mathf.Lerp(0, _dimPronounceness, t);
                _screenDimImage.color = newColor;
                yield return null;
            }

            _isScreenDimmed = true;
        }
    }

    private IEnumerator FadeScreen(float start, float end, VideoPlayer vp)
    {
        _screenDimGO.SetActive(true);
        float t = 0;
        Color newColor = Color.black;
        newColor.a = 0;

        while (t < _fadeDuration)
        {
            t += Time.deltaTime;
            newColor.a = Mathf.Lerp(start, end, t);
            _screenDimImage.color = newColor;
            yield return null;
        }

        PlayVideoPlayer(vp, false);
    }
}
