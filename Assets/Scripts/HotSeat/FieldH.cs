using Mono.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

//[0,0] - lower left angle
//Todo: test
public class FieldH : MonoBehaviour
{
    public enum Direction
    {
        Horizontal, Vertical, None
    }

    #region Prefabs and materials

    public TileH TileHPrefab;
    public Material StandardMaterial;
    public Material StartMaterial;
    public Material WordX2Material;
    public Material WordX3Material;
    public Material LetterX2Material;
    public Material LetterX3Material;

    #endregion Prefabs and materials

    public GameObject TimerImage;
    public Text TimerText;
    public Text Player1Text;
    public Text Player2Text;
    public GameObject EndGameCanvas;
    public UIController Controller;
    public Button SkipTurnButton;

    public UIGrid FieldGrid;
    public Direction CurrentDirection = Direction.None;
    public int CurrentTurn = 1;
    public bool isFirstTurn = true;
    public byte NumberOfRows = 15;
    public byte NumberOfColumns = 15;
    public LetterBoxH Player1;
    public LetterBoxH Player2;
    public byte CurrentPlayer = 1;
    public TileH[,] Field;
    public List<TileH> CurrentTiles;

    private int _turnsSkipped = 0;
    private SqliteConnection _dbConnection;
    private List<TileH> _wordsFound;
    private bool _timerEnabled;
    private int _timerLength;
    private float _timeRemaining;
    private List<TileH> _asterixTiles = new List<TileH>();

	//when the grid is initialized at first...
    private void Start()
    {
        CurrentTiles = new List<TileH>();//just a list of tile objects...
        var conection = @"URI=file:" + Application.streamingAssetsPath + @"/words.db";
        _dbConnection = new SqliteConnection(conection);
        _dbConnection.Open();
        _wordsFound = new List<TileH>();//just a list of tile objects...
        _timerEnabled = PlayerPrefs.GetInt("TimerEnabled") == 1;
        if (_timerEnabled)
        {
            TimerImage.SetActive(true);
            _timerLength = PlayerPrefs.GetInt("Length");
            _timeRemaining = (float)_timerLength + 1;
        }
        FieldGrid.Initialize();
        var letterSize = FieldGrid.Items[0, 0].gameObject.GetComponent<RectTransform>().rect.width;
        Player1.LetterSize = new Vector2(letterSize, letterSize);
        Player2.LetterSize = new Vector2(letterSize, letterSize);
        CreateField();
        Player1Text.text = PlayerPrefs.GetString("Player1", "Player 1");
        Player2Text.text = PlayerPrefs.GetString("Player2", "Player 2");
    }

    private void Update()
    {
        if (SkipTurnButton.interactable != (CurrentTiles.Count == 0))
            SkipTurnButton.interactable = CurrentTiles.Count == 0;
        if (Input.GetKeyDown(KeyCode.A))
            EndGame(null);
        if (_timerEnabled)
        {
            _timeRemaining -= Time.deltaTime;
            var timerValue = (int)_timeRemaining - 1;
            if (timerValue < 0)
                timerValue = 0;
            TimerText.text = timerValue.ToString();
            if (_timeRemaining < 0)
                OnEndTimer();
        }
    }

	//creating the tile grid and initializing the tiles as game objects...
    private void CreateField()
    {
        Field = new TileH[NumberOfRows, NumberOfColumns];
        for (var i = 0; i < NumberOfRows; i++)
        {
            for (var j = 0; j < NumberOfColumns; j++)
            {
                var newTile = Instantiate(TileHPrefab);
                newTile.transform.SetParent(gameObject.transform);
                newTile.Column = j;
                var render = newTile.GetComponent<Image>();
                render.material = StandardMaterial;
                newTile.Row = i;
                Field[i, j] = newTile;
                FieldGrid.AddElement(i, j, newTile.gameObject);
            }
        }
        Field[5, 5].CanDrop = true;//setting the start tile to the activate state...
        Field[5, 5].GetComponent<Image>().material = StartMaterial;//setting the red color to the start tile...
        AssignMaterials();
        AssignMultipliers();
    }

    #region Field generation

	//assigning colors(materials) to the different tiles in the grid(as different tiles score diffrently)...
    private void AssignMaterials()
    {
        Field[0, 0].GetComponent<Image>().material = WordX3Material;
        Field[0, 0].CurrentLetter.text = "TW";
        Field[0, 10].GetComponent<Image>().material = WordX3Material;
        Field[0, 10].CurrentLetter.text = "TW";
        Field[10, 0].GetComponent<Image>().material = WordX3Material;
        Field[10, 0].CurrentLetter.text = "TW";
        Field[10, 10].GetComponent<Image>().material = WordX3Material;
        Field[10, 10].CurrentLetter.text = "TW";
        Field[0, 5].GetComponent<Image>().material = WordX3Material;
        Field[0, 5].CurrentLetter.text = "TW";
        Field[10, 5].GetComponent<Image>().material = WordX3Material;
        Field[10, 5].CurrentLetter.text = "TW";
        Field[5, 0].GetComponent<Image>().material = WordX3Material;
        Field[5, 0].CurrentLetter.text = "TW";
        Field[7, 10].GetComponent<Image>().material = WordX3Material;
        Field[7, 10].CurrentLetter.text = "TW";
        for (var i = 1; i < 3; i++)
        {
            Field[i, i].GetComponent<Image>().material = WordX2Material;
            Field[i, i].CurrentLetter.text = "DW";
            Field[i, NumberOfRows - i - 1].GetComponent<Image>().material = WordX2Material;
            Field[i, NumberOfRows - i - 1].CurrentLetter.text = "DW";
            Field[NumberOfRows - i - 1, i].GetComponent<Image>().material = WordX2Material;
            Field[NumberOfRows - i - 1, i].CurrentLetter.text = "DW";
            Field[NumberOfRows - i - 1, NumberOfRows - i - 1].GetComponent<Image>().material = WordX2Material;
            Field[NumberOfRows - i - 1, NumberOfRows - i - 1].CurrentLetter.text = "DW";
        }
        Field[3, 1].GetComponent<Image>().material = LetterX3Material;
        Field[3, 1].CurrentLetter.text = "TL";
        Field[3, 3].GetComponent<Image>().material = LetterX3Material;
        Field[3, 3].CurrentLetter.text = "TL";
        Field[3, 7].GetComponent<Image>().material = LetterX3Material;
        Field[3, 7].CurrentLetter.text = "TL";
        Field[3, 9].GetComponent<Image>().material = LetterX3Material;
        Field[3, 9].CurrentLetter.text = "TL";
        Field[7, 1].GetComponent<Image>().material = LetterX3Material;
        Field[7, 1].CurrentLetter.text = "TL";
        Field[7, 3].GetComponent<Image>().material = LetterX3Material;
        Field[7, 3].CurrentLetter.text = "TL";
        Field[7, 7].GetComponent<Image>().material = LetterX3Material;
        Field[7, 7].CurrentLetter.text = "TL";
        Field[7, 9].GetComponent<Image>().material = LetterX3Material;
        Field[7, 9].CurrentLetter.text = "TL";
        Field[1, 3].GetComponent<Image>().material = LetterX3Material;
        Field[1, 3].CurrentLetter.text = "TL";
        Field[1, 7].GetComponent<Image>().material = LetterX3Material;
        Field[1, 7].CurrentLetter.text = "TL";
        Field[9, 3].GetComponent<Image>().material = LetterX3Material;
        Field[9, 3].CurrentLetter.text = "TL";
        Field[9, 7].GetComponent<Image>().material = LetterX3Material;
        Field[9, 7].CurrentLetter.text = "TL";
        Field[0, 3].GetComponent<Image>().material = LetterX2Material;
        Field[0, 3].CurrentLetter.text = "DL";
        Field[0, 7].GetComponent<Image>().material = LetterX2Material;
        Field[0, 7].CurrentLetter.text = "DL";
        Field[10, 3].GetComponent<Image>().material = LetterX2Material;
        Field[10, 3].CurrentLetter.text = "DL";
        Field[10, 7].GetComponent<Image>().material = LetterX2Material;
        Field[10, 7].CurrentLetter.text = "DL";
        Field[2, 4].GetComponent<Image>().material = LetterX2Material;
        Field[2, 4].CurrentLetter.text = "DL";
        Field[2, 6].GetComponent<Image>().material = LetterX2Material;
        Field[2, 6].CurrentLetter.text = "DL";
        Field[8, 4].GetComponent<Image>().material = LetterX2Material;
        Field[8, 4].CurrentLetter.text = "DL";
        Field[8, 6].GetComponent<Image>().material = LetterX2Material;
        Field[8, 6].CurrentLetter.text = "DL";
        Field[3, 0].GetComponent<Image>().material = LetterX2Material;
        Field[3, 0].CurrentLetter.text = "DL";
        Field[3, 5].GetComponent<Image>().material = LetterX2Material;
        Field[3, 5].CurrentLetter.text = "DL";
        Field[3, 10].GetComponent<Image>().material = LetterX2Material;
        Field[3, 10].CurrentLetter.text = "DL";
        Field[7, 0].GetComponent<Image>().material = LetterX2Material;
        Field[7, 0].CurrentLetter.text = "DL";
        Field[7, 5].GetComponent<Image>().material = LetterX2Material;
        Field[7, 5].CurrentLetter.text = "DL";
        Field[7, 10].GetComponent<Image>().material = LetterX2Material;
        Field[7, 10].CurrentLetter.text = "DL";
        Field[4, 2].GetComponent<Image>().material = LetterX2Material;
        Field[4, 2].CurrentLetter.text = "DL";
        Field[5, 7].GetComponent<Image>().material = LetterX2Material;
        Field[5, 7].CurrentLetter.text = "DL";
        Field[6, 2].GetComponent<Image>().material = LetterX2Material;
        Field[6, 2].CurrentLetter.text = "DL";
        Field[4, 8].GetComponent<Image>().material = LetterX2Material;
        Field[4, 8].CurrentLetter.text = "DL";
        Field[6, 8].GetComponent<Image>().material = LetterX2Material;
        Field[6, 8].CurrentLetter.text = "DL";
        Field[5, 3].GetComponent<Image>().material = LetterX2Material;
        Field[5, 3].CurrentLetter.text = "DL";
        Field[5, 7].GetComponent<Image>().material = LetterX2Material;
        Field[5, 7].CurrentLetter.text = "DL";
    }

	//assigning score values according to the colors assigned to the tiles in the grid...
    private void AssignMultipliers()
    {
        Field[0, 0].WordMultiplier = 3;
        Field[0, 10].WordMultiplier = 3;
        Field[10, 0].WordMultiplier = 3;
        Field[10, 10].WordMultiplier = 3;
        Field[0, 5].WordMultiplier = 3;
        Field[10, 5].WordMultiplier = 3;
        Field[5, 0].WordMultiplier = 3;
        Field[7, 10].WordMultiplier = 3;
       
        for (var i = 1; i < 3; i++)
        {
            Field[i, i].WordMultiplier = 2;
            Field[i, NumberOfRows - i - 1].WordMultiplier = 2;
            Field[NumberOfRows - i - 1, i].WordMultiplier = 2;
            Field[NumberOfRows - i - 1, NumberOfRows - i - 1].WordMultiplier = 2;
        }
        Field[3, 1].LetterMultiplier = 3;
        Field[3, 3].LetterMultiplier = 3;
        Field[3, 7].LetterMultiplier = 3;
        Field[3, 9].LetterMultiplier = 3;
        Field[7, 1].LetterMultiplier = 3;
        Field[7, 3].LetterMultiplier = 3;
        Field[7, 7].LetterMultiplier = 3;
        Field[7, 9].LetterMultiplier = 3;
        Field[1, 3].LetterMultiplier = 3;
        Field[1, 7].LetterMultiplier = 3;
        Field[9, 3].LetterMultiplier = 3;
        Field[9, 7].LetterMultiplier = 3;

        Field[0, 3].LetterMultiplier = 2;
        Field[0, 7].LetterMultiplier = 2;
        Field[10, 3].LetterMultiplier = 2;
        Field[10, 7].LetterMultiplier = 2;
        Field[2, 4].LetterMultiplier = 2;
        Field[2, 6].LetterMultiplier = 2;
        Field[8, 4].LetterMultiplier = 2;
        Field[8, 6].LetterMultiplier = 2;
        Field[3, 0].LetterMultiplier = 2;
        Field[3, 5].LetterMultiplier = 2;
        Field[3, 10].LetterMultiplier = 2;
        Field[7, 0].LetterMultiplier = 2;
        Field[7, 5].LetterMultiplier = 2;
        Field[7, 10].LetterMultiplier = 2;
        Field[4, 2].LetterMultiplier = 2;
       
    }

    #endregion Field generation

	//when the clock time is zero...
    private void OnEndTimer()
    {
        _timeRemaining = (float)_timerLength + 1;
        OnRemoveAll();
        OnSkipTurn();
    }

    public void OnEndTurn()
    {
		//print ("clicked it");
        if (CurrentTiles.Count > 0)
        {
			
			if (CheckWords ()) {//if successfully words are being checked
				
				_turnsSkipped = 0;
				CurrentTurn++;
				var points = CountPoints ();
				if (CurrentPlayer == 1) {
					Player1.ChangeBox (7 - Player1.CurrentLetters.Count);
					Player1.Score += points;
					if (Player1.CurrentLetters.Count == 0) {
						EndGame (Player1);
					}
					Player1.gameObject.SetActive (false);
					Player2.gameObject.SetActive (true);
					CurrentTiles.Clear ();
					CurrentDirection = Direction.None;
					CurrentPlayer = 2;
					Controller.InvalidatePlayer (1, Player1.Score);
					isFirstTurn = false;
				} else {
					Player2.ChangeBox (7 - Player2.CurrentLetters.Count);
					Player2.Score += points;
					if (Player2.CurrentLetters.Count == 0)
						EndGame (Player2);
					Player1.gameObject.SetActive (true);
					Player2.gameObject.SetActive (false);
					CurrentDirection = Direction.None;
					CurrentTiles.Clear ();
					CurrentPlayer = 1;
					Controller.InvalidatePlayer (2, Player2.Score);
					isFirstTurn = false;
				}
				if (_timerEnabled)
					_timeRemaining = (float)_timerLength + 1;
			} else {
                this.OnRemoveAll(); // go back tiles when word is not exist
                if(!Field[5, 5].HasLetter)
                {
                    Field[5, 5].CanDrop = true;
                }
                Controller.ShowNotExistError ();

			}
        }
        else 
			Controller.ShowZeroTilesError();
		
        _wordsFound = new List<TileH>();
    }

    public void OnChangeLetters()
    {
        if (CurrentPlayer == 1)
        {
            if (!Player1.ChangeLetters())
            {
                Controller.ShowChangeLetterError();
                return;
            }
            _turnsSkipped = 0;
            CurrentDirection = Direction.None;
            Player1.gameObject.SetActive(false);
            Player2.gameObject.SetActive(true);
            CurrentPlayer = 2;
            Controller.InvalidatePlayer(1, Player1.Score);
            CurrentTiles.Clear();
        }
        else
        {
            if (!Player2.ChangeLetters())
            {
                Controller.ShowChangeLetterError();
                return;
            }
            _turnsSkipped = 0;
            CurrentDirection = Direction.None;
            Player1.gameObject.SetActive(true);
            Player2.gameObject.SetActive(false);
            CurrentPlayer = 1;
            Controller.InvalidatePlayer(2, Player2.Score);
            CurrentTiles.Clear();
        }
        if (_timerEnabled)
            _timeRemaining = (float)_timerLength + 1;
    }

	//when skipping turns...
    public void OnSkipTurn()
    {
        CurrentDirection = Direction.None;
        if (CurrentPlayer == 1)
        {
            Player1.gameObject.SetActive(false);
            Player2.gameObject.SetActive(true);
            CurrentPlayer = 2;
            Controller.InvalidatePlayer(1, Player1.Score);
        }
        else
        {
            Player1.gameObject.SetActive(true);
            Player2.gameObject.SetActive(false);
            CurrentPlayer = 1;
            Controller.InvalidatePlayer(2, Player2.Score);
        }
        if (_timerEnabled)
            _timeRemaining = (float)_timerLength + 1;
        if (++_turnsSkipped == 4) //if the number of turns skipped is 4 game ends...
            EndGame(null);
    }

	//removing the current tiles... 
    public void OnRemoveAll()
    {
        for (var i = CurrentTiles.Count - 1; i >= 0; i--)
        {
            CurrentTiles[i].RemoveTile();
        }
        CurrentTiles.Clear();
        CurrentDirection = Direction.None;
    }

    #region Word checking

	//returns true if word checking is successfull...
    private bool CheckWords()
    {
        var words = CreateWords();//returns a list with start and end letters...
        _wordsFound = words;
        var word = GetWord(words[0], words[1]);//return a string...
        if (_asterixTiles.Count != 0)
        {
            var index1 = word.IndexOf('_');//get the index of the underscore...
            var index2 = 0;
            if (_asterixTiles.Count == 2)
                index2 = word.IndexOf('_', index1 + 1);//get the other index if asterix count is 2...
            var variants = GetAllWordVariants(word);//get all the words included "word" in the database...
            SwitchDirection();
            foreach (var variant in variants)//loop through similar words found
            {
                _asterixTiles[0].TempLetter = variant[index1].ToString();
                if (_asterixTiles.Count == 2)
                    _asterixTiles[1].TempLetter = variant[index2].ToString();
                var successful = true;
                for (var i = 3; i < words.Count; i += 2)//loop until all the words array is finished
                {
                    word = GetWord(words[i - 1], words[i]);
                    if (!CheckWord(word))//gets the count of the word apperas in the database is zero
                    {
                        successful = false;
                        break;
                    }
                }
                if (successful)
                {
                    SwitchDirection();
                    return true;
                }
            }
            SwitchDirection();
            return false;
        }
        else//asterix count is zero
        {
            var successful = CheckWord(word);
            var i = 3;
            SwitchDirection();
            while (successful && i < words.Count)
            {
                word = GetWord(words[i - 1], words[i]);
                successful = CheckWord(word);
                i += 2;
            }
            SwitchDirection();
            return successful;
        }
    }

	//returns the start and end tiles array...
    private List<TileH> CreateWords()
    {
        _asterixTiles.Clear();
        var res = new List<TileH>();//results list of tiles...
        if (CurrentDirection == Direction.None)
            CurrentDirection = Direction.Horizontal;
        TileH start, end;
        CreateWord(CurrentTiles[0], out start, out end);//checks the start and end tiles...
        if (start == end)//no word has made...
        {
            SwitchDirection();
            CreateWord(CurrentTiles[0], out start, out end);
        }
        res.Add(start);
        res.Add(end);
        SwitchDirection();
		//look for every tile in current tiles list and start and end tiles to res list...
        foreach (var tile in CurrentTiles)
        {
            CreateWord(tile, out start, out end);
            if (start != end)
            {
                res.Add(start);
                res.Add(end);
            }
        }
        if (_asterixTiles.Count == 2)
            _asterixTiles = _asterixTiles.OrderByDescending(t => t.Row).ThenBy(t => t.Column).Distinct().ToList();
        SwitchDirection();
        return res;
    }

    private void CreateWord(TileH start, out TileH wordStart, out TileH wordEnd)
    {
        if (CurrentDirection == Direction.Vertical)
        {
            var j = start.Row;
			//go vertically down while there is no letter...
            while (j < 15 && Field[j, start.Column].HasLetter)
            {
                if (Field[j, start.Column].CurrentLetter.text.Equals("*"))//if a astrix meets while going down
                    if (!_asterixTiles.Contains(Field[j, start.Column]))//if astrixtiles array does not contain that tile
                        _asterixTiles.Add(Field[j, start.Column]);//add it to the array...
                j++;
            }
            wordStart = Field[j - 1, start.Column];//get the word start position... 
            j = start.Row;
			//go vertically up while there is no letter...
            while (j >= 0 && Field[j, start.Column].HasLetter)
            {
                if (Field[j, start.Column].CurrentLetter.text.Equals("*"))
                    if (!_asterixTiles.Contains(Field[j, start.Column]))
                        _asterixTiles.Add(Field[j, start.Column]);
                j--;
            }
            wordEnd = Field[j + 1, start.Column];//get the word end position...
        }
        else//search horizontally...
        {
            var j = start.Column;
			//go horizontally rightwards...
            while (j >= 0 && Field[start.Row, j].HasLetter)
            {
                if (Field[start.Row, j].CurrentLetter.text.Equals("*"))
                    if (!_asterixTiles.Contains(Field[start.Row, j]))
                        _asterixTiles.Add(Field[start.Row, j]);
                j--;
            }
            wordStart = Field[start.Row, j + 1];
            j = start.Column;
			//go horizontally backwords...
            while (j < 15 && Field[start.Row, j].HasLetter)
            {
                if (Field[j, start.Column].CurrentLetter.text.Equals("*"))
                    if (!_asterixTiles.Contains(Field[start.Row, j]))
                        _asterixTiles.Add(Field[start.Row, j]);
                j++;
            }
            wordEnd = Field[start.Row, j - 1];
        }
    }

    private int CountPoints()
    {
        var result = 0;
        var wordMultiplier = 1;
        var score = new int[_wordsFound.Count / 2];
        for (var i = 0; i < _wordsFound.Count; i += 2)
        {
            var tempRes = 0;
            if (_wordsFound[i].Row == _wordsFound[i + 1].Row)
                for (var j = _wordsFound[i].Column; j <= _wordsFound[i + 1].Column; j++)
                {
                    var tile = Field[_wordsFound[i].Row, j];
                    tempRes += LetterBoxH.PointsDictionary[tile.CurrentLetter.text] * tile.LetterMultiplier;
                    if (tile.LetterMultiplier != 0)
                        tile.LetterMultiplier = 1;
                    wordMultiplier *= tile.WordMultiplier;
                    tile.WordMultiplier = 1;
                }
            else
            {
                for (var j = _wordsFound[i].Row; j >= _wordsFound[i + 1].Row; j--)
                {
                    var tile = Field[j, _wordsFound[i].Column];
                    tempRes += LetterBoxH.PointsDictionary[tile.CurrentLetter.text] * tile.LetterMultiplier;
                    if (tile.LetterMultiplier != 0)
                        tile.LetterMultiplier = 1;
                    wordMultiplier *= tile.WordMultiplier;
                    tile.WordMultiplier = 1;
                }
            }
            result += tempRes;
            score[i / 2] = tempRes;
        }
        var start = 7 + _wordsFound.Count / 2;
        foreach (var i in score)
        {
            Field[start, 0].SetPoints(i * wordMultiplier);
            start--;
        }
        if (_asterixTiles.Count != 0)
            ApplyAsterixLetters();
        return result * wordMultiplier;
    }

    private void ApplyAsterixLetters()
    {
        foreach (var tile in _asterixTiles)
        {
            tile.CurrentLetter.text = tile.TempLetter;
            tile.TempLetter = null;
            tile.LetterMultiplier = 0;
            tile.WordMultiplier = 1;
        }
    }

    private void SwitchDirection()
    {
        CurrentDirection = CurrentDirection == Direction.Horizontal ? Direction.Vertical : Direction.Horizontal;
    }

	//building the word with start and end tiles given...
    private string GetWord(TileH begin, TileH end)
    {
        if (CurrentDirection == Direction.Vertical)
        {
            //var sb = new StringBuilder();
			string sb = "";
            for (var j = begin.Row; j >= end.Row; j--)
            {
				if (!String.IsNullOrEmpty (Field [j, begin.Column].TempLetter))//if the letter is not null...
                    //sb.Append(Field[j, begin.Column].TempLetter);//append it to the string...
					sb += Field [j, begin.Column].TempLetter;
				else if (Field [j, begin.Column].CurrentLetter.text.Equals ("*"))
                    //sb.Append('_');
					sb += "_";
				else {
					//sb.Append (Field [j, begin.Column].CurrentLetter.text);
					sb += Field [j, begin.Column].CurrentLetter.text;
				}//if null or empty append it...

            }
            return sb;
        }
        else//build word horizontaly...
        {
            //var sb = new StringBuilder();
			string sb = "";
            for (var j = begin.Column; j <= end.Column; j++)
            {
				if (!String.IsNullOrEmpty (Field [begin.Row, j].TempLetter))
                    //sb.Append(Field[begin.Row, j].TempLetter);
					sb += Field [begin.Row, j].TempLetter;
				else if (Field [begin.Row, j].CurrentLetter.text.Equals ("*"))
					sb += "_";
				else {
					//sb.Append (Field [begin.Row, j].CurrentLetter.text);
					sb += Field [begin.Row, j].CurrentLetter.text;
				}
            }
            return sb;
        }
    }

	//returns a list with the given word included in the database...
    private List<string> GetAllWordVariants(string word)
    {
        /* var sql = "SELECT * FROM AllWords WHERE Word like \"" + word.ToLower() + "\"";
         var command = new SqliteCommand(sql, _dbConnection);
         var reader = command.ExecuteReader();
         if (reader.HasRows)
         {
             var res = new List<string>();
             while (reader.Read())
             {
                 res.Add(reader.GetString(0));
             }
             reader.Close();
             return res;
         }
         reader.Close();
         return null;*/

		List<string> res = new List<string>(new string[] { "අර", "රට", "මල", "කමත", "කර" });
		return res;
        /*var res = new List<string>() {"bad","bcd","ab" };
        return res;*/
        

    }

	//returns true if the word count found in the db is not zero
    private bool CheckWord(string word)
    {
        /*var sql = "SELECT count(*) FROM AllWords WHERE Word like \"" + word.ToLower() + "\"";
        var command = new SqliteCommand(sql, _dbConnection);
        var inp = command.ExecuteScalar();
        return Convert.ToInt32(inp) != 0;*///gets the equivalent 32 bit number and returns it if not equal to zero

        //loading the text files and searching the word

       

        TextAsset textasset = (TextAsset)Resources.Load("sinhala_wordset");

        string content = textasset.text;
        string txt;
        System.IO.StringReader reader = null;

        List<string> lines = new List<string>();

        reader = new System.IO.StringReader(textasset.text);
        if (reader == null)
        {
            Debug.Log("sinhala_wordset.txt not found or not readable");
        }
        else
        {
            // Read each line from the file
            while ((txt = reader.ReadLine()) != null)
                //Debug.Log("-->" + txt);
                lines.Add(txt.Trim());

        }

        if (lines.Contains(word))
        {
            print("yess");
            return true;
        }
       
       
        return false;





        /* string[] stringArray= {"අර","රට","මල","කමත","කර" };
         print (word);

         int pos = Array.IndexOf (stringArray,word);
         if (pos > -1) {
             print ("sucess");
             return true;
         } else {
             print ("false");
             return false;
         }*/

    }

    #endregion Word checking

    private void EndGame(LetterBoxH playerOut)//Player, who ran out of letters is passed
    {
        var tempPoints = Player1.RemovePoints();
        tempPoints += Player2.RemovePoints();
        if (playerOut != null)
        {
            playerOut.Score += tempPoints;
        }
        var winner = Player1.Score > Player2.Score ? 1 : 2;
        Controller.SetWinner(winner, Player1.Score, Player2.Score, Player1Text.text, Player2Text.text);
    }
}