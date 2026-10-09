"""Shared corrected milk-shop joinery, fitted to the original real wall aperture."""
def build_windows(box,pane,pale,glass):
    # Existing wall rows leave Z=1.032..2.317, planar Y=-.60...80 open.
    # Cover the returns by 5 cm; protrude 7 cm beyond both wall faces.
    bottom,top=1.082,2.267
    centre=(bottom+top)/2
    for side in [-1,1]:
        x=side*3.60
        for y in [-.63,.83]:box('WindowFrame',(x,y,centre),(.30,.16,top-bottom+.32),pale,'Windows')
        for z in [bottom-.08,top+.08]:box('WindowFrame',(x,.10,z),(.30,1.30,.16),pale,'Windows')
        box('WindowSill',(side*3.66,.10,.89),(.38,1.78,.11),pale,'Windows')
        box('WindowMullion',(x,.10,centre),(.16,.055,top-bottom),pale,'Windows')
        # Butt crossbars to the mullion, never overlay its same-depth front face.
        for low,high in [(-.55,.0725),(.1275,.75)]:
            box('WindowCrossbar',(x,(low+high)/2,centre),(.16,high-low,.055),pale,'Windows')
            for zlow,zhigh in [(bottom,centre-.0275),(centre+.0275,top)]:
                pane('SingleOldGlassPane',x,(low+high)/2,(zlow+zhigh)/2,high-low-.004,zhigh-zlow-.004,glass,'Windows')
