using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TheSudokuSlayer
{
    public partial class Form1 : Form
    {
        public const int CellWidth = 32;
        public const int CellHeight = 32;

        //offset from the top-left corner of the window
        private const int XOffset = -20;
        private const int YOffset = 25;

        //color for empty cell
        private readonly Color _defaultBackColor = Color.White;

        private readonly Color _fixedBackColor = Color.White;
        private readonly Color _fixedForeColor = Color.Black;

        private readonly Color _userBackColor = Color.White;
        private readonly Color _userForeColor = Color.Black;

        public static string FileContents;

        public static string SaveFileName;

        public static int SelectedNumber;

        public static bool GameStarted;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = string.Empty;
            toolStripStatusLabel2.Text = string.Empty;

            // draw the board
            DrawBoard();
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            toolStripStatusLabel2.Text = $@"Elapsed time: {_seconds} seconds(s)";
        }

        public Stack<string> Moves { get; set; } = new Stack<string>();

        private Stack<string> RedoMoves { get; set; } = new Stack<string>();

        // used to represent the values in the grid
        private bool _hintMode;

        private readonly int[,] _actual = new int[10, 10];

        private readonly string[,] _possible = new string[10, 10];

        private string CalculatePossibleValues(int column, int row)
        {
            var isInvalidInput = (column > 9 || column < 1 || row > 9 || row < 1);
            if (isInvalidInput)
            {
                throw new Exception("Out of range invariables for CalculatePossibleValues function");
            }

            // get the possible values for a cell
            var str = _possible[column, row] == string.Empty ? "123456789" : _possible[column, row];

            // Step(1) check by column
            for (var r = 1; r <= 9; r++)
            {
                // this means the cell has an _actual value
                if (_actual[column, r] != 0)
                {
                    str = str.Replace(_actual[column, r].ToString(), string.Empty);
                }
            }

            // Step(2) check by row
            for (var c = 1; c <= 9; c++)
                // this means the cell has an _actual value
                if (_actual[c, row] != 0)
                {
                    str = str.Replace(_actual[c, row].ToString(), string.Empty);
                }

            // Step(3) check by minigrid
            var startColumn = column - (column - 1) % 3;
            var endColumn = startColumn + 2;
            var startRow = row - (row - 1) % 3;
            var endRow = startRow + 2;
            for (var rr = 1; startRow <= endRow; startRow++)
            {
                for (var cc = 1; startColumn <= endColumn; startColumn++)
                {
                    // this means the cell has an _actual value
                    if (_actual[cc, rr] != 0)
                    {
                        str = str.Replace(_actual[cc, rr].ToString(), string.Empty);
                    }
                }
            }

            // if possible value is an empty string there is an error because of an invalid move
            if (str == string.Empty) throw new Exception("Invalid Move");

            return str;
        }

        public Stack<string> PossibleActualStack { get; set; } = new Stack<string>();

        private int _seconds;

        private void ClearBoard()
        {
            Moves = new Stack<string>();
            RedoMoves = new Stack<string>();

            // to initialize the cells in the board we need to set all eraseable cell values to zero
            for (int row = 1; row <= 9; row++)
            {
                for (int column = 1; column <= 9; column++)
                {
                    SetCell(column, row, 0, 1);
                }
            }
        }

        private void Form_Paint(object sender, PaintEventArgs e)
        {
            int y1;
            int y2;

            // draw the horizontal lines
            var x1 = 1 * (CellWidth + 1) + XOffset - 1;
            var x2 = 9 * (CellWidth + 1) + XOffset + CellWidth;

            for (var row = 1; row <= 10; row += 3)
            {
                y1 = row * (CellHeight + 1) + YOffset - 1;
                y2 = y1;
                e.Graphics.DrawLine(Pens.Red, x1, y1, x2, y2);
            }

            // draw the vertical lines
            y1 = 1 * (CellHeight + 1) + YOffset - 1;
            y2 = 9 * (CellHeight + 1) + YOffset + CellHeight;

            var step = 3;
            for (var column = 1; column <= 10; column += step)
            {
                x1 = column * (CellWidth + 1) + XOffset - 1;
                x2 = x1;
                e.Graphics.DrawLine(Pens.Red, x1, y1, x2, y2);
            }
        }

        private void SetCell(int col, int row, int value, int eraseable)
        {
            //Locate the particular Label control
            var lbl = Controls.Find(col.ToString() + row, true).FirstOrDefault();
            var cellLabel = (Label)lbl;

            if (cellLabel == null) return;

            //save the value in the array
            _actual[col, row] = value;

            //if erasing a cell, you need to reset the possible values for all cells
            int[] rows = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            int[] columns = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            if (value == 0)
            {
                foreach (var r in rows)
                {
                    foreach (var c in columns)
                    {
                        if (_actual[c, r] == 0) _possible[c, r] = string.Empty;
                    }
                }
            }

            // means user-set values 
            if (value != 0 && eraseable == 1)
            {
                cellLabel.Text = value.ToString();
                cellLabel.BackColor = _userBackColor;
                cellLabel.Font = new Font(cellLabel.Font, FontStyle.Regular);
                cellLabel.ForeColor = _userForeColor;
            }

            ////set the appearance for the Label control---
            //if (value == 0)
            //{
            //    cellLabel.Text = string.Empty;
            //    cellLabel.Tag = eraseable;
            //    cellLabel.BackColor = _defaultBackColor;

            //}

            // this means default puzzle values
            if (value != 0 && eraseable == 0)
            {
                cellLabel.Tag = null;
                cellLabel.Text = value.ToString();
                cellLabel.Font = new Font(cellLabel.Font, FontStyle.Bold);
                cellLabel.BackColor = _fixedBackColor;
                cellLabel.ForeColor = _fixedForeColor;
            }

            if (value == 0)
            {
                cellLabel.Text = string.Empty;
            }

        }

        private void DrawBoard()
        {
            // default selected number is 1
            toolStripButton1.Checked = true;
            SelectedNumber = 1;

            // used to store the location of the cell
            var location = new Point();
            for (var row = 1; row <= 9; row++)
                for (var column = 1; column <= 9; column++)
                {
                    location.X = column * (CellWidth + 1) + XOffset;
                    location.Y = row * (CellHeight + 1) + YOffset;

                    var label = new Label
                    {
                        Name = column.ToString() + row,
                        BorderStyle = BorderStyle.Fixed3D,
                        Location = location,
                        Width = CellWidth,
                        Height = CellHeight,
                        TextAlign = ContentAlignment.MiddleCenter,
                        BackColor = _defaultBackColor,
                        Font = new Font(Font, FontStyle.Bold),
                        Tag = "1"
                    };

                    label.Click += Cell_Click;
                    Controls.Add(label);
                }
        }

        private void ToolStripButton_Click(object sender, EventArgs e)
        {
            foreach (var item in toolStrip1.Items)
                // uncheck all of the buttons in the ToolStrip
                if (item is ToolStripButton toolStripButton)
                {
                    toolStripButton.Checked = false;
                }

            SelectedNumber = ((ToolStripButton)sender).Text == @"Erase"
                ? 0
                : int.Parse(((ToolStripButton)sender).Text);
        }

        private void ButtonHint_Click(object sender, EventArgs e)
        {
            // show hints one cell at a time
            _hintMode = true;
            try
            {
                SolvePuzzle();
            }
            catch (Exception)
            {
                MessageBox.Show(@"Please undo your move", @"Invalid Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonSolvePuzzle_Click(object sender, EventArgs e)
        {
            // solve the puzzle
            _hintMode = false;
            try
            {
                SolvePuzzle();
            }
            catch (Exception)
            {
                MessageBox.Show(@"Please undo your move", @"Invalid Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SolvePuzzle()
        {
            try
            {
                // perform column/row and minigrid elimination
                bool changes;
                do
                {
                    do
                    {
                        do
                        {
                            do
                            {
                                changes = CheckColumnsAndRows();
                                if (_hintMode && changes || IsPuzzleSolved())
                                {
                                    break;
                                }

                            } while (_hintMode && changes || IsPuzzleSolved());

                            changes = LookForLoneRangersInMinigrids();
                            if (_hintMode && changes || IsPuzzleSolved())
                            {
                                break;
                            }
                        } while (_hintMode && changes || IsPuzzleSolved());

                        changes = LookForLoneRangersInRows();
                    } while (_hintMode && changes || IsPuzzleSolved());

                    changes = LookForLoneRangersInColumns();
                } while (_hintMode && changes || IsPuzzleSolved());
            }
            catch (Exception)
            {
                throw new Exception("Invalid Move");
            }

            if (IsPuzzleSolved())
            {
                timer1.Enabled = false;
                Console.Beep();
                toolStripStatusLabel1.Text = @"******************Puzzle Solved******************";
                MessageBox.Show(@"Puzzle solved");
            }
        }

        private bool CheckColumnsAndRows()
        {
            var changes = false;
            int[] rows = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            int[] columns = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            // check all cells
            foreach (var row in rows)
                foreach (var column in columns)
                    if (_actual[column, row] == 0)
                    {
                        if (_actual[column, row] == 0) _possible[column, row] = CalculatePossibleValues(column, row);

                        // display the _possible values in the ToolTip
                        SetToolTip(column, row, _possible[column, row]);

                        if (_possible[column, row].Length == 1)
                        {
                            // that means a number is confirmed
                            SetCell(column, row, int.Parse(_possible[column, row]), 1);

                            // number is confirmed
                            _actual[column, row] = int.Parse(_possible[column, row]);
                            DisplayActivity("Column/Row and Minigrid Elimination", false);
                            DisplayActivity("==================================", false);
                            DisplayActivity($"Inserted value {_actual[column, row]} in ({column},{row})", false);

                            // get the UI of the application to refresh with the newly confirmed number
                            Application.DoEvents();

                            // save the move into the Moves stack
                            Moves.Push($"{column}{row}{_possible[column, row]}");

                            // if the user asks for a hint, stop at this point
                            changes = true;
                            if (_hintMode) return true;
                        }
                    }

            return changes;
        }

        public bool LookForLoneRangersInRows()
        {
            bool changes = false;
            int columnPosition = 0;
            int rowPosition = 0;

            // check by row
            for (int r = 1; r <= 9; r++)
            {
                for (int n = 1; n <= 9; n++)
                {
                    var occurrence = 0;
                    for (int c = 1; c <= 9; c++)
                    {
                        if (_actual[c, r] == 0 && _possible[c, r].Contains(n.ToString()))
                        {
                            occurrence += 1;
                            // if multiple occurrences, not a lone ranger anymore
                            if (occurrence > 1) break;
                            columnPosition = c;
                            rowPosition = r;
                        }
                    }

                    if (occurrence == 1)
                    {
                        // number is confirmed
                        SetCell(columnPosition, rowPosition, n, 1);
                        SetToolTip(columnPosition, rowPosition, n.ToString());
                        // pushed the move onto the stack
                        Moves.Push($"{columnPosition}{rowPosition}{n}");
                        DisplayActivity("Look for Lone Rangers in Rows", false);
                        DisplayActivity("================================", false);
                        DisplayActivity("Inserted value {n.ToString()} in ({columnPosition},{rowPosition})", false);
                        Application.DoEvents();
                        changes = true;

                        // if user clicks the Hint button, exit the function
                        if (_hintMode) return true;
                    }
                }
            }

            return changes;
        }

        public bool LookForLoneRangersInColumns()
        {
            bool changes = false;
            int columnPosition = 0;
            int rowPosition = 0;

            // check by column
            for (int c = 1; c <= 9; c++)
            {
                for (int n = 1; n <= 9; n++)
                {
                    var occurrence = 0;
                    for (int r = 1; r <= 9; r++)
                    {
                        if (_actual[c, r] == 0 && _possible[c, r].Contains(n.ToString()))
                        {
                            occurrence += 1;
                            // if multiple occurrences, not a lone ranger anymore
                            if (occurrence > 1) break;
                            columnPosition = c;
                            rowPosition = r;
                        }
                    }

                    if (occurrence == 1)
                    {
                        // number is confirmed
                        SetCell(columnPosition, rowPosition, n, 1);
                        SetToolTip(columnPosition, rowPosition, n.ToString());
                        // pushes the move onto the stack
                        Moves.Push($"{columnPosition}{rowPosition}{n}");
                        DisplayActivity("Look for Lone Rangers in Columns", false);
                        DisplayActivity("================================", false);
                        DisplayActivity("Inserted value {n.ToString()} in ({columnPosition},{rowPosition})", false);
                        Application.DoEvents();
                        changes = true;

                        // if user clicks the Hint button, exit the function
                        if (_hintMode) return true;
                    }
                }
            }

            return changes;
        }

        public bool LookForLoneRangersInMinigrids()
        {
            bool changes = false;
            int columnPosition = 0;
            int rowPosition = 0;

            // check for each number from 1 to 9
            for (int n = 1; n <= 9; n++) // for n
            {
                // check the 9 minigrids
                for (int r = 1; r <= 9; r += 3) // r
                {
                    for (int c = 1; c <= 9; c += 3) // for c
                    {
                        var nextMinigrid = false;
                        // check within the minigrid
                        var occurrence = 0;
                        for (int rr = 0; rr <= 2; rr++) // for rr
                        {
                            for (int cc = 0; cc <= 2; cc++) // for cc
                            {
                                if (_actual[c + cc, r + rr] == 0 &&
                                    _possible[c + cc, r + rr].Contains(n.ToString())) // If
                                {
                                    occurrence += 1;
                                    columnPosition = c + cc;
                                    rowPosition = r + rr;
                                    if (occurrence > 1) // If
                                    {
                                        nextMinigrid = true;
                                        break;
                                    } // End If
                                } // End If
                            } // Next cc

                            if (nextMinigrid) // If
                            {
                                break;
                            } // End If
                        } // Next rr

                        if (!nextMinigrid && occurrence == 1) // If
                        {
                            // that means number is confirmed
                            SetCell(columnPosition, rowPosition, n, 1);
                            SetToolTip(columnPosition, rowPosition, n.ToString());
                            // saves the move into the stack
                            Moves.Push($"{columnPosition}{rowPosition}{n}");
                            DisplayActivity("Look for Lone Rangers in Minigrids", false);
                            DisplayActivity("==================================", false);
                            DisplayActivity($"Inserted value {n} in ({columnPosition},{rowPosition})", false);
                            Application.DoEvents();
                            changes = true;
                            // if user clicks the Hint button, exit the function
                            if (_hintMode) // If
                            {
                                return true;
                            } // End If
                        } // End If
                    } // Next c integer
                } // Next r integer
            } // Next n integer

            return changes;
        } // End LookForLoneRangersInMinigrids

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var caption = "Save current game";
            var text = "Do you want to save current game?";

            var response = MessageBox.Show(text, caption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            switch (response)
            {
                case DialogResult.Yes:
                    SaveGameToDisk(false);
                    break;
                case DialogResult.Cancel:
                    Application.Exit();
                    break;
            }
        }

        private void NewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!GameStarted)
            {
                StartNewGame();
            }
            else
            {
                var response = MessageBox.Show(@"Do you want to save current game?",
                    @"Save Game",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (response == DialogResult.Yes)
                {
                    SaveGameToDisk(false);
                }
            }
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (GameStarted)
            {
                var response = MessageBox.Show(@"Do you want to save current game?",
                    @"Save current game",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                switch (response)
                {
                    case DialogResult.Yes:
                        SaveGameToDisk(false);
                        break;
                    case DialogResult.Cancel:
                        return;
                }
            }

            // load the game from disk
            using (var openFileDialog1 = new OpenFileDialog
            {
                InitialDirectory = @"C:\Users\greg\Documents\Projects\Sudoku\Sudoku\",
                Filter = @"SDO files (*.sdo)|*.sdo|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = false
            })
            {
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    FileContents = File.ReadAllText(openFileDialog1.FileName);

                    // load the game from disk
                    toolStripStatusLabel1.Text = openFileDialog1.FileName;
                    SaveFileName = openFileDialog1.FileName;
                }
                else
                {
                    return;
                }
            }

            StartNewGame();

            // initialize the board from the file contents that were created by saving a game
            var counter = 0;
            for (var r = 1; r <= 9; r++)
                for (var c = 1; c <= 9; c++)
                {
                    if (FileContents.Length != 0)
                    {
                        SetCell(c, r, int.Parse(FileContents[counter].ToString()), 0);
                    }

                    counter += 1;
                }

        }

        private void RedoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // if there are no previous moves on then exit
            if (RedoMoves.Count == 0)
            {
                return;
            }

            // remove from the _redoMoves stack push onto the _moves stack
            var str = RedoMoves.Pop();
            Moves.Push(str);

            // save the value in the array
            SetCell(int.Parse(str.Substring(0)), int.Parse(str.Substring(1)), int.Parse(str.Substring(2)), 1);
            DisplayActivity($"Value reinserted at {int.Parse(str.Substring(0))}, {int.Parse(str.Substring(1))}", false);
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (GameStarted)
            {
                SaveGameToDisk(false);
            }
            else
            {
                DisplayActivity("Game not started yet", true);
            }
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (GameStarted)
            {
                SaveGameToDisk(true);
            }
            else
            {
                DisplayActivity(@"Game not started yet", true);
            }
        }

        private void UndoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Moves.Count != 0)
            {
                // if there are no previous moves then exit
                var str = Moves.Pop();
                RedoMoves.Push(str);
                // save the value in the array
                SetCell(int.Parse(str.Substring(0)), int.Parse(str.Substring(1)), int.Parse(str.Substring(2)), 1);
                // remove from the _moves stack push onto the _redoMove stack
                DisplayActivity($"Value reinserted at {int.Parse(str.Substring(0))}, {int.Parse(str.Substring(1))}",
                    false);
            }
        }

        private void SaveGameToDisk(bool saveAs)
        {
            // if _saveFileName is empty it means the game has not been saved before
            if (SaveFileName == string.Empty || saveAs)
            {
                using (var dialog = new SaveFileDialog
                {
                    Filter = @"SDO files (*.sdo)|*.sdo|All files (*.*)|*.*",
                    FilterIndex = 1,
                    RestoreDirectory = false
                })
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        SaveFileName = dialog.FileName;
                    }
                    else
                    {
                        return;
                    }
                }

                // create the string representing the values to store 
                var sb = new StringBuilder();
                for (var row = 1; row <= 9; row++)
                    for (var column = 1; column <= 9; column++)
                    {
                        sb.Append(_actual[column, row].ToString());
                    }

                // save the values to the file
                var fileExists = File.Exists(SaveFileName);
                if (fileExists)
                {
                    File.Delete(SaveFileName);
                    File.WriteAllText(@"_saveFileName", sb.ToString());
                }
            }
        }

        public bool BruteForceStop { get; set; }

        private bool IsPuzzleSolved()
        {
            string pattern = "123456789";
            bool isSolved = true;

            // check row by row
            for (int r = 1; r <= 9; r++)
            {
                pattern = "123456789";
                for (int c = 1; c <= 9; c++)
                {
                    pattern = pattern.Replace(_actual[c, r].ToString(), string.Empty);
                }
                if (pattern.Length > 0)
                {
                    isSolved = false;
                }
            }

            // check column by column
            for (int c = 1; c <= 9; c++)
            {
                pattern = "123456789";
                for (int r = 1; r <= 9; r++)
                {
                    pattern = pattern.Replace(_actual[c, r].ToString(), string.Empty);
                }
                if (pattern.Length > 0)
                {
                    isSolved = false;
                }
            }

            // check by mini-grid
            for (int c = 1; c <= 9; c += 3)
            {
                pattern = "123456789";
                for (int r = 1; r <= 9; r += 3)
                {
                    for (int cc = 0; cc <= 2; cc++)
                    {
                        for (int rr = 0; rr <= 2; rr++)
                        {
                            pattern = pattern.Replace(_actual[c + cc, r + rr].ToString(), string.Empty);

                        }
                    }
                }
            }
            if (pattern.Length > 0)
            {
                return false;
            }

            return isSolved;
        }

        private void DisplayActivity(string str, bool soundBeep)
        {
            if (soundBeep)
            {
                Console.Beep();
            }

            txtActivities.Text += str + Environment.NewLine;
        }

        private bool IsMoveValid(int column, int row, int value)
        {
            bool isMoveValid = false;
            // scan through columns
            for (int r = 1; r <= 9; r++)
            {
                // then it is a duplicate
                isMoveValid = _actual[column, r] != value;
            }

            // scan through rows
            for (int c = 1; c <= 9; c++)
            {
                // then it is a duplicate
                isMoveValid = _actual[c, row] != value;

                // scan through mini-grid
                var startColumn = column - ((column - 1) % 3);
                var startRow = row - ((row - 1) % 3);
                for (int rr = 0; rr <= 2; rr++)
                {
                    for (int cc = 0; cc <= 2; cc++)
                    {
                        // then it is a duplicate
                        isMoveValid = _actual[startColumn + cc, startRow + rr] != value;
                    }
                }
            }

            return isMoveValid;
        }

        private void StartNewGame()
        {
            SaveFileName = string.Empty;
            txtActivities.Text = string.Empty;
            _seconds = 0;
            ClearBoard();
            GameStarted = true;
            timer1.Enabled = true;
            toolStripStatusLabel1.Text = @"New game started";
            toolTip1.RemoveAll();
        }

        private void SetToolTip(int col, int row, string possibleValues)
        {
            // locate the particular label control
            var control = Controls.Find(col.ToString() + row, true).FirstOrDefault();
            toolTip1.SetToolTip((Label)control ?? throw new InvalidOperationException(), possibleValues);
        }

        private void Cell_Click(object sender, EventArgs e)
        {
            Label cellLabel = (Label)sender;

            // check to see if game has even started or not---
            if (!GameStarted)
            {
                DisplayActivity("Click File->New to start a new game or File->Open to load an existing game", true);
            }


            // if cell is not erasable then exit
            if (cellLabel.Tag != null && cellLabel.Tag.ToString() == "0")
            {
                DisplayActivity(@"Selected cell is not empty", false);
                return;
            }

            // determine the col and row of the selected cell---
            int col = int.Parse(cellLabel.Name.Substring(0, 1));
            int row = int.Parse(cellLabel.Name.Substring(1, 1));

            // If erasing a cell---
            if (SelectedNumber == 0)
            {
                // if cell is empty then no need to erase---
                if (_actual[col, row] == 0)
                {
                    return;
                }

                // save the value in the array
                SetCell(col, row, SelectedNumber, 1);
                DisplayActivity($"Number erased at ({col},{row})", false);
            }
            else if (cellLabel.Text == string.Empty)
            {
                if (!IsMoveValid(col, row, SelectedNumber))
                {
                    DisplayActivity("Invalid move at (" + col + "," + row + ")", false);
                    Console.Beep();
                    return;
                }

                // save the value in the array
                SetCell(col, row, SelectedNumber, 1);
                DisplayActivity($"{SelectedNumber} placed at ({col},{row})", false);

                // saves the move into the stack
                Moves.Push(cellLabel.Name + SelectedNumber);

                if (!IsPuzzleSolved()) return;
                timer1.Enabled = false;
                Console.Beep();
                toolStripStatusLabel1.Text = @"*****Puzzle Solved*****";
            }
        }

    }
}
