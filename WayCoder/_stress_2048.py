import random
import game_2048 as g

random.seed(42)
game = g.Game()
steps = 0
while game.can_move() and steps < 5000:
    d = random.choice(['left', 'right', 'up', 'down'])
    game.move(d)
    assert all(0 <= v for row in game.board for v in row), 'negative value!'
    assert all(len(row) == 4 for row in game.board), 'bad width'
    steps += 1

with open('_stress_result.txt', 'w') as f:
    f.write(f'steps={steps} score={game.score} max_tile={game.max_tile()}\n')
print('done')
