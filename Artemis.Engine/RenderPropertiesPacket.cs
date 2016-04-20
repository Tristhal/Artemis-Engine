﻿
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Artemis.Engine
{
    public class RenderPropertiesPacket
    {
        public SpriteSortMode SpriteSortMode { get; set; }
        public BlendState BlendState { get; set; }
        public SamplerState SamplerState { get; set; }
        public DepthStencilState DepthStencilState { get; set; }
        public RasterizerState RasterizerState { get; set; }
        public Effect Effect { get; set; }
        public Matrix? Matrix { get; set; }

        public RenderPropertiesPacket( SpriteSortMode ssm    = SpriteSortMode.Deferred
                                     , BlendState bs         = null
                                     , SamplerState ss       = null
                                     , DepthStencilState dss = null
                                     , RasterizerState rs    = null
                                     , Effect e              = null
                                     , Matrix? m             = null)
        {
            SpriteSortMode = ssm;
            BlendState = bs;
            SamplerState = ss;
            DepthStencilState = dss;
            RasterizerState = rs;
            Effect = e;
            Matrix = m;
        }
    }
}
