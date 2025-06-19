
def solve(lis):
    initial_list = lis[:]
    count = 1
    x = 0
    for el in lis:
        x ^= el
    lis.append(x)
    lis.pop(0)

    while (lis != initial_list):
        x = 0
        for el in lis:
            x = x ^ el
        lis.append(x)
        lis.pop(0)
        count += 1

    if(count > len(lis) + 1): print("HEHEEHE", lis)


# Execute

lis = [0, 1, 0, 1, 1, 0, 1, 0, 1, 1, 0, 0]
for j in range(1, 1000000):
    lis = []
    i = j
    while (i):
        if (i % 2):
            lis.append(1)
        else:
            lis.append(0)
        i = i // 2
    solve(lis)


# [0, 1, 0, 1, 1, 0, 1, 0, 1, 1, 0, 0]
# [1, 0, 1, 1, 0, 1, 0, 1, 1, 0, 0, 0]
# [0, 1, 1, 0, 1, 0, 1, 1, 0, 0, 0, 0]
# [1, 1, 0, 1, 0, 1, 1, 0, 0, 0, 0, 1]
# [1, 0, 1, 0, 1, 1, 0, 0, 0, 0, 1, 0]
# [0, 1, 0, 1, 1, 0, 0, 0, 0, 1, 0, 1]
# [1, 0, 1, 1, 0, 0, 0, 0, 1, 0, 1, 1]
# [0, 1, 1, 0, 0, 0, 0, 1, 0, 1, 1, 0]
# [1, 1, 0, 0, 0, 0, 1, 0, 1, 1, 0, 1]
# [1, 0, 0, 0, 0, 1, 0, 1, 1, 0, 1, 0]
# [0, 0, 0, 0, 1, 0, 1, 1, 0, 1, 0, 1]
# [0, 0, 0, 1, 0, 1, 1, 0, 1, 0, 1, 1]
# [0, 0, 1, 0, 1, 1, 0, 1, 0, 1, 1, 0]