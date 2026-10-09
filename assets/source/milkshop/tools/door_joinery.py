"""Shared metre-scale, non-overlapping front doorway joinery."""
def build_door_frame(box, material, front=-2.9):
    bottom, lintel_bottom, gap = .16, 2.73, .003
    top = lintel_bottom-gap
    # Existing outside face retained; inside face clears wall (+.08) by 8cm.
    centre_y, depth = front-.02, .36
    for x in (-1.31,1.31):
        box('DoorJamb',(x,centre_y,(bottom+top)/2),(.16,depth,top-bottom),material,'Trim')
    box('DoorLintel',(0,centre_y,2.82),(2.80,depth,.18),material,'Trim')
    return dict(insideProjectionM=.08,buttGapM=gap,lintelBottomM=lintel_bottom,jambTopM=top)
