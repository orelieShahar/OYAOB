﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TexasHoldem.Logic.User;

namespace TexasHoldem.Logic.Game
{
    public class ConcreteGamePrefDecorator : GamePrefDecorator
    {
        public ConcreteGamePrefDecorator(string name, int sb, int bb, int minMoney, int maxMoney, int gameNumber) : base(name, sb, bb, minMoney, maxMoney, gameNumber)
        {
        }

        private void Fold()
        {
            throw new NotImplementedException();
        }

        private void Raise(int sum)
        {
            throw new NotImplementedException();
        }

        private void Check()
        {
            throw new NotImplementedException();
        }

        private void Call()
        {
            throw new NotImplementedException();
        }

        private Player findWinner(int sum)
        {
            throw new NotImplementedException();
        }
    }
}
