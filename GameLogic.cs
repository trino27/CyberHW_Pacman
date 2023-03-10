using System;
using System.Collections.Generic;
using System.Threading;

namespace CyberHW_Pacmen
{
    class GameLogic
    {
        private Map map = new Map();
        private User user;
        private List<Enemy> enemies;
        private List<Thread> enemiesParameterizedThreads;
        private Thread timerParameterizedThread;
        private Object locker = -1;
        private int enemy_thread_exist_num=0;

        private bool already_lose = false;
        private bool isPause = false;

        public int CurrentTime = 0;
        public int CurrentLevel = 0;

        public GameLogic()
        {
            lock (locker)
            {
                enemiesParameterizedThreads = new List<Thread>();
                InitNewMap();
                timerParameterizedThread = new Thread(new ParameterizedThreadStart(TimerAction));
                timerParameterizedThread.Start(this.map.Level.GetSecLimit);
            }
        }

        private void TimerAction(object obj_sec)
        {
            do
            {
                IsTimeOver();
                if (!isPause)
                {
                    Thread.Sleep(1000);
                    CurrentTime++;
                }
            } while (true);

        }
        private void EnemyAction(object enemy_index)
        {
            int index = (int)enemy_index;
            this.enemies[index].LastWay = enemies[index].ChosingWay();
            Enemy.Way new_way;

            while (!already_lose && index < enemies.Count)
            {
                if (!isPause)
                {
                    if (map.IsPosAvailable(this.enemies[index].ProcessingWay(this.enemies[index].LastWay)))
                    {
                        this.enemies[index].Move(this.enemies[index].ProcessingWay(this.enemies[index].LastWay));
                        this.map.NewCreationPos(this.enemies[index]);
                        if (Thread.CurrentThread.IsAlive)
                        {
                            Thread.Sleep(this.enemies[index].GetSpeed);
                        }
                    }
                    else
                    {
                        new_way = enemies[index].ChosingWay();
                        if (map.IsPosAvailable(this.enemies[index].ProcessingWay(new_way)))
                        {
                            this.enemies[index].LastWay = new_way;
                            this.enemies[index].Move(this.enemies[index].ProcessingWay(new_way));
                            this.map.NewCreationPos(this.enemies[index]);
                            if (Thread.CurrentThread.IsAlive)
                            {
                                Thread.Sleep(this.enemies[index].GetSpeed);
                            }
                        }
                    }
                    if (IsKilled()) already_lose = true;
                }
            }
        }

        public void UserAction(ConsoleKey key)
        {

            if ((ConsoleKey.W == key || ConsoleKey.S == key || ConsoleKey.D == key || ConsoleKey.A == key) && !isPause)
            {
                if (map.IsPosAvailable(this.user.ProcessingKey(key)))
                {
                    this.user.LastKey = key;
                    this.user.Move(this.user.ProcessingKey(key));
                    this.map.NewCreationPos(this.user);
                    Thread.Sleep(this.user.GetSpeed);
                }
                else if (map.IsPosAvailable(this.user.ProcessingKey(this.user.LastKey)) && this.user.LastKey != ConsoleKey.O)
                {
                    this.user.Move(this.user.ProcessingKey(this.user.LastKey));
                    this.map.NewCreationPos(this.user);
                    Thread.Sleep(this.user.GetSpeed);
                }
            }
            else if (key == ConsoleKey.P && !isPause)
            {
                if (isPause != true) isPause = true;

            }
            else if (key == ConsoleKey.U && isPause)
            {
                isPause = false;
                if ((map.IsPosAvailable(this.user.ProcessingKey(this.user.LastKey)) && this.user.LastKey != ConsoleKey.O))
                {
                    this.user.Move(this.user.ProcessingKey(this.user.LastKey));
                    this.map.NewCreationPos(this.user);
                    Thread.Sleep(this.user.GetSpeed);
                }
            }
            else
            {
                if ((map.IsPosAvailable(this.user.ProcessingKey(this.user.LastKey)) && this.user.LastKey != ConsoleKey.O) && !isPause)
                {
                    this.user.Move(this.user.ProcessingKey(this.user.LastKey));
                    this.map.NewCreationPos(this.user);
                    Thread.Sleep(this.user.GetSpeed);
                }
            }

        }
        public void MapAction()
        {
            do
            {
                lock (locker)
                {
                    map.ShowMap();
                    GameInfo();
                }
            } while (true);

        }

        private void RemoveAllEnemy()
        {
            lock (locker)
            {
                enemiesParameterizedThreads.Clear();
                enemies = new List<Enemy>();
            }
        }
        private void InitNewMap()
        {
            lock (locker)
            {
                Console.Clear();
                this.map.InitMap(CurrentLevel);
                this.user = map.InitUser();

                if (!already_lose)
                {
                    enemies = map.InitEnemies();
                    InitEnemyThreads();
                }
                this.user.LastKey = ConsoleKey.O;
            }
        }
        private void InitEnemyThreads()
        {
            lock (locker)
            {
                for (int i = 0; i < enemy_thread_exist_num; i++)
                {
                    this.enemiesParameterizedThreads.Add(new Thread(new ParameterizedThreadStart(EnemyAction)));
                }
                    for (int i = enemy_thread_exist_num; i < this.enemies.Count; i++)
                {
                    this.enemiesParameterizedThreads.Add(new Thread(new ParameterizedThreadStart(EnemyAction)));
                    enemy_thread_exist_num++;
                    this.enemiesParameterizedThreads[i].Start(i);
                }
            }
        }
        private void GameInfo()
        {
            Console.SetCursorPosition(0, map.Level.area_size_y);
            if (CurrentLevel == 0)
            {
                Console.WriteLine("Press D to start...\n");
            }
            if (this.map.Level.IsHaveTimeLimit)
            {
                Console.WriteLine($"Time(sec): {CurrentTime}/{this.map.Level.GetSecLimit}");
            }
            else Console.WriteLine("Time(sec): Unlimited");
            Console.WriteLine($"Level: {CurrentLevel}\nScore: {map.UserScore}\n");
            Console.Write("W - UP\nS - DOWN\nD - RIGHT\nA - LEFT\nP - Pause\nU - Continue\nEnter - Restart\nEsc - End");
            
        }

        public bool IsTimeOver()
        {
            if (this.map.Level.IsHaveTimeLimit)
            {
                lock (locker)
                {
                    if (CurrentTime >= this.map.Level.GetSecLimit)
                    {
                        return true;
                    }
                    return false;
                }
            }
            else return false;
        }
        public bool IsKilled()
        {
            lock (locker)
            {
                foreach (var i in this.enemies)
                {
                    if (this.user.position_x == i.position_x && this.user.position_y == i.position_y)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        public bool IsWin()
        {
            lock (locker)
            {
                if (this.user.position_x == this.map.Level.finish_pos_x && this.user.position_y == this.map.Level.finish_pos_y) return true;
                else return false;
            }
        }

        public void Lose()
        {
            lock (locker)
            {
                already_lose = true;
                NextLevel(-1);
            }
        }
        public void NextLevel()
        {
            lock (locker)
            {
                RemoveAllEnemy();

                CurrentTime = 0;
                CurrentLevel++;

                InitNewMap();
            }
        }
        public void NextLevel(int lvl_i)
        {
            lock (locker)
            {
                RemoveAllEnemy();

                CurrentTime = 0;
                CurrentLevel = lvl_i;

                InitNewMap();
            }
        }
        public void Restart()
        {
            lock (locker)
            {
                RemoveAllEnemy();
                CurrentLevel = 0;
                enemy_thread_exist_num = 0;
                map.UserScore = 0;
                CurrentTime = 0;
                already_lose = false;
                isPause = false;
                InitNewMap();
            }
        }
        public void EndGame()
        {
            lock (locker)
            {
                already_lose = true;
                if (timerParameterizedThread.IsAlive) timerParameterizedThread.IsBackground = true;
                RemoveAllEnemy();
            }
        }

    }
}
