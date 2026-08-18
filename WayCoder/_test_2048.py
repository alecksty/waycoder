import game_2048 as g

game = g.Game()
cases = [
    ([2, 2, 0, 0], ([4, 0, 0, 0], 4)),
    ([2, 2, 2, 0], ([4, 2, 0, 0], 4)),
    ([2, 2, 2, 2], ([4, 4, 0, 0], 8)),
    ([4, 4, 8, 8], ([8, 16, 0, 0], 24)),
    ([0, 0, 0, 0], ([0, 0, 0, 0], 0)),
    ([2, 4, 8, 16], ([2, 4, 8, 16], 0)),
    ([4, 2, 4, 2], ([4, 2, 4, 2], 0)),
]
ok = True
for inp, (el, es) in cases:
    line, score = game._slide(inp)
    if line != el or score != es:
        ok = False
        print('FAIL', inp, '->', line, score, 'want', el, es)
print('slide tests:', 'ALL PASS' if ok else 'SOME FAIL')

# right direction
line, score = game._slide([0, 0, 2, 2])
print('right [0,0,2,2] ->', line, score)

# move integration
game.reset()
print('initial nonzero:', 16 - sum(row.count(0) for row in game.board))
print('move left ->', game.move('left'))
print('max tile:', game.max_tile())
print('can move:', game.can_move())
