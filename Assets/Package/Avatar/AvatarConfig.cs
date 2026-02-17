using System;

namespace Foundry
{
    [Serializable]
    public struct AvatarConfig
    {
        // 0 = Male, 1 = Female
        public byte gender;

        // Index into available hairstyles
        public byte hair;

        // Index into skin tone options (or later a float)
        public byte skinTone;
    }
}