using UnityEngine;
using Zenject;

namespace Zenject.SpaceFighter
{
    public class EnemyHealthWatcher : ITickable
    {
        readonly GameEvents _gameEvents;
        readonly Explosion.Factory _explosionFactory;
        readonly Context _context;
        readonly EnemyModel _model;

        public EnemyHealthWatcher(
            EnemyModel model,
            Context context,
            Explosion.Factory explosionFactory,
            GameEvents gameEvents)
        {
            _gameEvents = gameEvents;
            _explosionFactory = explosionFactory;
            _context = context;
            _model = model;
        }

        public void Tick()
        {
            if (_model.Health <= 0)
            {
                Die();
            }
        }

        void Die()
        {
            var explosion = _explosionFactory.Spawn();
            explosion.transform.position = _model.Position;

            GameObject.Destroy(_context.gameObject);

            _gameEvents.EnemyKilled(_model);
        }
    }
}
