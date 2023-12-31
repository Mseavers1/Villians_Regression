using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Source.Visual_Novel
{
    public class VisualGameManager : MonoBehaviour
    {
        public TMP_Text textBox, nameBox;
        public GameObject[] buttons;
        public Canvas canvas;
        public string textFile;
        public Color defaultTextColor, highlightedTextColor;
        public GameObject historyObj, conObj, settingObj;
        
        public bool isSkimming, isAuto;

        private readonly Queue<string> _loadedTexts = new();
        private readonly LinkedList<string> _loadedNames = new ();
        private readonly Queue<string> _previousTexts = new();
        private readonly Queue<string> _previousNames = new();

        private double _timer, _autoTimer;
        private float _savedTextSpeed, _savedAutoSpeed;
        private bool _isRunning, _isOverButton, _savedIsAuto, _isOnHistory, _isOnSettings;
        private int _currentIndex;

        private PlayerInput _input;
        private GraphicRaycaster _caster;
        private PointerEventData _pointerEventData;
        private List<RaycastResult> _results;
        private int _currentHoverButton;

        public Queue<string> GetPreviousText() { return _previousTexts; }
        public Queue<string> GetPreviousNames() { return _previousNames; }

        private void Awake()
        {
            _input = GetComponent<PlayerInput>();
            _caster = canvas.GetComponent<GraphicRaycaster>();
            _pointerEventData = new PointerEventData(EventSystem.current);
            _results = new List<RaycastResult>();
        }

        private void Start()
        {
            _input.onActionTriggered += MouseHandler;
            
            LoadTextFile();
            PlayText();
        }

        private void Update()
        {
            // Checks if auto is enabled
            if (isAuto && !_isRunning)
            {
                _autoTimer += Time.deltaTime;
                
                if (_autoTimer >= PlayerPrefs.GetFloat("AutoSpeed")) PlayText();
            }
            
            // Checks if the system is running
            if (!_isRunning) return;
            
            _timer += Time.deltaTime;

            if (_timer >= PlayerPrefs.GetFloat("TextSpeed")) NextCharacter();
        }

        private void FixedUpdate()
        {
            var pos = Mouse.current.position.ReadValue();
            _pointerEventData.position = pos;
            _results.Clear();
            
            _caster.Raycast(_pointerEventData, _results);

            _isOverButton = false;
            foreach (var result in _results.Where(result => result.gameObject.CompareTag("Button")))
            {
                var obj = result.gameObject.name.Split(' ');
                SwitchHover(int.Parse(obj[1]));
                _isOverButton = true;
            }
            
            if (!_isOverButton) SwitchHover(-1);
        }

        private void SwitchHover(int id)
        {
            if (id == -1) _isOverButton = false;
            _currentHoverButton = id;
            
            for (var index = 0; index < buttons.Length; index++)
            {
                buttons[index].GetComponent<TMP_Text>().color = index == id ? highlightedTextColor : defaultTextColor;
            }
        }

        private void LoadTextFile()
        {
            try
            {
                _loadedTexts.Clear();

                var path = "Assets/Resources/VisualNovel Texts/" + textFile + ".txt";
                var reader = new StreamReader(path);
                var line = reader.ReadLine();
                var speaker = "None";

                while (line != null)
                {
                    if (line.Length == 0 || line[0] == '/')
                    {
                        line = reader.ReadLine();
                        continue;
                    }

                    if (line[0] == '*')
                    {
                        var lines = line.Split(' ');
                        speaker = "";

                        for (var i = 1; i < lines.Length; i++)
                        {
                            speaker += lines[i];

                            if (i + 1 < lines.Length) speaker += " ";

                        }
                        
                        line = reader.ReadLine();
                        continue;
                    }
                    
                    _loadedTexts.Enqueue(line);
                    _loadedNames.AddLast(speaker);
                    line = reader.ReadLine();
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private void MouseHandler(InputAction.CallbackContext context)
        {
            if (context.action.name.Equals("Left Click") && context.performed) OnLeftClick();
        }

        private void OnLeftClick()
        {
            // Check if it is clicking a button
            if (_isOverButton)
            {
                switch (_currentHoverButton)
                {
                    case 0:
                        if (isSkimming || _isOnSettings) return;
                        
                        // History
                        _isOnHistory = !_isOnHistory;

                        if (_isOnHistory)
                        {
                            historyObj.SetActive(true);
                            historyObj.GetComponent<HistoryController>().GenerateHistory();
                            _savedIsAuto = isAuto;
                            isAuto = false;
                        }
                        else
                        {
                            historyObj.SetActive(false);
                            isAuto = _savedIsAuto;
                        }
                        
                        break;
                    case 1:
                        if (historyObj.activeSelf || _isOnSettings) return;
                        
                        // Auto
                        isAuto = !isAuto;
                        break;
                    case 2:
                        if (historyObj.activeSelf || _isOnSettings) return;
                        
                        // Skim
                        isSkimming = !isSkimming;
                        SkimmingUpdate();
                        
                        break;
                    case 3:
                        // Skip
                        break;
                    case 4:
                        // Settings
                        _isOnSettings = !_isOnSettings;
                        settingObj.SetActive(!settingObj.activeSelf);

                        break;
                }
                return;
            }

            if (historyObj.activeSelf || _isOnSettings) return;
            
            // Checks if text is running, if so, skips to the end.
            if (_isRunning)
            {
                textBox.text = _loadedTexts.Peek();
                EndText();
            }

            // Checks if no text is running, if so, moves to the next line
            else
            {
                conObj.GetComponent<ConAnimation>().AnimationOff();
                conObj.SetActive(false);
                
                PlayText();
            }
        }

        private void SkimmingUpdate()
        {
            if (isSkimming)
            {
                _savedTextSpeed = PlayerPrefs.GetFloat("TextSpeed");
                _savedAutoSpeed = PlayerPrefs.GetFloat("AutoSpeed");
                _savedIsAuto = isAuto;

                PlayerPrefs.SetFloat("TextSpeed", 0);
                PlayerPrefs.SetFloat("AutoSpeed", 0.001f);

                isAuto = true;
            }
            else
            {
                PlayerPrefs.SetFloat("TextSpeed", _savedTextSpeed);
                PlayerPrefs.SetFloat("AutoSpeed", _savedAutoSpeed);
                isAuto = _savedIsAuto;
            }
        }

        // Loads the next set of text into the system
        private void PlayText()
        {
            if (_loadedTexts.Count <= 0)
            {
                conObj.GetComponent<ConAnimation>().AnimationOff();
                conObj.SetActive(false);
                
                isSkimming = false;
                return;
            }
            
            _timer = 0;
            _autoTimer = 0;
            _currentIndex = 0;
            _isRunning = true;

            textBox.text = "";
            nameBox.text = _loadedNames.First();
            
            NextCharacter();
        }

        // Loads the current char of the loaded text into the text box
        private void NextCharacter()
        {
            if (PlayerPrefs.GetFloat("TextSpeed") == 0)
            {
                textBox.text += _loadedTexts.Peek();
                EndText();
                return;
            }
            
            textBox.text += _loadedTexts.Peek()[_currentIndex];

            _timer = 0;
            _currentIndex++;
            
            if (_currentIndex >= _loadedTexts.Peek().Length) EndText();
        }

        // Ends the system
        private void EndText()
        {
            _isRunning = false;
            _previousTexts.Enqueue(_loadedTexts.Dequeue());
            _previousNames.Enqueue(_loadedNames.First());
            _loadedNames.RemoveFirst();
            
            conObj.SetActive(true);
            conObj.GetComponent<ConAnimation>().AnimationOn();
        }
    }
}
