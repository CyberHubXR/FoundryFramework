

using UnityEngine;

namespace Foundry
{
    public class PlayerRigManagerService : IPlayerRigManager
    {
        public IPlayerControlRig Rig => _rig;
        public event PlayerRigEvent PlayerRigCreated;
        public event PlayerRigEvent PlayerRigBorrowed;
        public event PlayerRigEvent PlayerRigReturned;

        private bool _rigBorrowed;
        private IPlayerControlRig _rig;
        private Transform _unusedRigHolder;
    
    
        public void RegisterRig(IPlayerControlRig rig, Transform unusedRigHolder)
        {
            Debug.Assert(_rig == null, "Cannot register more than one player rig!");
            _rig = rig;
            _unusedRigHolder = unusedRigHolder;
            PlayerRigCreated?.Invoke(rig);
        }

        public IPlayerControlRig BorrowPlayerRig()
        {
            Debug.Assert(_rig != null, "Player rig was borrowed before it was created!");
            Debug.Assert(!_rigBorrowed, "There can only be one player rig at a time!");
            _rigBorrowed = true;
            PlayerRigBorrowed?.Invoke(_rig);
            return _rig;
        }

        public void ReturnPlayerRig()
        {
            Debug.Assert(_rigBorrowed, "Player rig was returned without being borrowed!");
            _rigBorrowed = false;
        
            //Just in-case this got caught in a parent's deactivation
            _rig.transform.gameObject.SetActive(true);
            _rig.enabled = true;
            _rig.transform.SetParent(_unusedRigHolder, false);
            PlayerRigReturned?.Invoke(_rig);
        }
    }
}