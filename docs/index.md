% NOVA Documentation
% Johannes Wagner
% 06/27/2016

# Introduction {#introduction}

The **N(On) Verbal Annotator ** (NOVA) offers a graphical interface for machine-aided annotation of large multi-modal databases. It is developed as a part of the Social Signal Interpretation (SSI) framework to describe databases recorded with SSI (but also externaly recorded databases). It's key features are:

* **Visualization and playback** of multi-modal data including support for skeleton and face tracking
* Annotation based on **various schemes** (e.g. discrete and continuous)
* **Database back-end** to share annotations between multiple annotators
* Integrated tools to apply machine-aided **cooperative learning**

# Installation

The latest version of NOVA can be accessed at:

https://github.com/hcmlab/nova

Just download or check-out the release branch and start 'nova.exe' (yes, NOVA is available for Windows systems only).

If you do not see videos on playback, please make also sure you have the right codecs installed. A good choice is the K-lite Codec Pack (You can leave all settings on default, but make sure to not not install any 3rd party software that comes with the installer)

# Manual Annotation

In the following we will explain the basic steps to manually create and edit an annotation. 

## Main Interface

The following image shows NOVA's main interface. On the top, media files are displayed in horizontally aligned boxes. Videos and tracking data (e.g. facial points) are handled as media and displayed frame by frame. Below that other stream data (e.g. audio waveforms) are displayed as time series in vertically aligned tracks. In the same way, annotation data is visualized in vertical tiers. Below the annotation tiers a slider bar allows it to move the displayed clip along the time scale. The clip can be enlarged or shrink by changing the size of the scroll button. Double-clicking the scroll button will zoom out to show the whole session at once (note: if navigation becomes slow try zooming in). Playback buttons are found on the bottom of the window. A red cursor marks the current playback position (it will be moved during playback and when the signal track is clicked). A green cursor shows the current position on the annotation tier and helps to align labels with the signal tracks. A list of annotations items of the currently selected annotation tier is shown on the left. When an item is selected by the red cursor is moved to the according segment in the tier. Only one media, stream and annotation can be selected at a time. When an entity is clicked which is not already selected, it receives the focus (ie.g. its border turns dark gray) and additional information is displayed in the status bar.

![*NOVA's interface: from top down media, streams and annotations are displayed. On the left a list of annotation items of the selected annotation tier is shown. Time-line and the navigation panel are found at the bottom.*](pics/manual-interface.png){#fig:manual-interface width=100%}

## Loading and Removing Files

To add a file the FILE menu can be used. It allows to select multiple media, stream or annotation files at once. Alternatively, files or folders can be dropped from the explorer. NOVA supports all common video formats, audio wave files and SSI stream files, as well as, CSV files, which will be imported as stream files (in the latter case the user has to provide number format and sample rate). The currently selected media, stream or annotation file can be removed by clicking the 'x' in the according status bar. Clicking the 'Clear' button on the right bottom will remove all files at once. The current workspace can be stored to a project file and reloaded at a later point from the FILE menu (or via drag and drop).

## Creating an Annotation

After loading at least one media or data stream, new annotations tracks can be added to the project. New annotations are created by clicking the 'File' button (bottom left). Alternatively, annotations can be created from the database, too, see REF. A window pops up, which allows to select a scheme type:

![*New annotation scheme window.*](pics/manual-new-scheme.png){#fig:manual-new-scheme width=40%}

* Discrete:

Discrete annotations consist of a list of labelled time segments. Each segment has a start and and end time (in seconds) and a name (label). Segments can be of varying length, may overlap and possibly there are gaps between adjacent segments. An annotator cannot change the names of labels and has to assign exactly one label to each segment. If none of the labels is applicable the label "GARBAGE" is always available.

![*Example of a discrete annotation tier.*](pics/manual-types-of-annotation-discrete.png){#fig:manual-types-of-annotation-discrete width=100%}

Scheme name, class names and colours are set in the following dialogue:

![*New discrete annotation scheme window.*](pics/manual-new-scheme-discrete.png){#fig:manual-new-scheme-discrete}

* Free:

Like discrete annotations, but this time annotators are free to choose the label names. This is obviously useful if an annotation task can not easily be reduced to a few labels (for example in case of speech transcriptions). Of course there is the risk that the same phenomenon may be labelled differently (either because a synonym is used or due to misspelling).

![*Example of a free annotation tier.*](pics/manual-types-of-annotation-free.png){#fig:manual-types-of-annotation-free width=100%}

* Continuous:

In contrast to discrete annotations, continuous annotations assign numerical values (label score) instead of label names. Also, they have a fixed sample rate, i.e. label scores are assigned in regular intervals. For instance, a sample of 2 Hz means that an annotator has to assign two scores each second. All score values have to be within a fixed interval (defined by a minimum and a maximum score). Optionally, it is possible to quantize the interval into limited steps. The value NAN is assigned when no score is applicable.

![*Example of a continuous annotation tier.*](pics/manual-types-of-annotation-continuous.png){#fig:manual-types-of-annotation-continuous width=100%}

Scheme name, sample rate, value range and colours are set in the following dialogue:

![*New continuous annotation scheme window.*](pics/manual-new-scheme-continuous.png){#fig:manual-new-scheme-continuous}


## Editing an Annotation

* Free:

To place a new segment right click on the start position of the label, keep button pressed and move the cursor to the end position (or vice-versa). Now release the button and a new segment will be added (and selected). If 'Force Label' is active you will be asked to enter label name and confidence. Otherwise it gets the label that was previously used. To edit an existing segment left click inside the segment (the colour of the selected segment will change to blue). If you now move the cursor near the borders two arrows will be displayed and you can change the position by holding down the left mouse button. If you move the cursor towards the center of the segment you can move the whole segment along the track. To change the label hit the 'W' key. Note that you can change the position of a label during playback. If 'Follow Annotation' is active a newly added segment will be immediately played back. Alternatively, you can also jump to a segment by selecting it from the list left to the annotation tiers. It is also possible to select several segments in the list and rename them all at once.

* Discrete:

Works in the same way as free annotations, but when entering or changing a label a selection dialogue pops up that allows you to select a name from the loaded scheme. Alternatively, you can change the label name of a selected segment by pressing a key between '0' and '9'. For example, if the selected scheme contains three names 'A','B' and 'C' pressing '1' assigns 'A', '2' assigns 'B', and '3' assigns 'C'. Any other numbers and '0' assign the garbage class.

* Continuous:

To change the values in a continuous track hold down the right mouse button and move the cursor to the desired position within the track. You will notice that the values immediately start to follow the cursor. Hitting 'Ctrl' turns on the live annotation mode. Now a white button is displayed at the left border of the current track and only the value at the current playback position (red marker) will follow your vertical mouse movement (horizontal position of the mouse is ignored). It is no longer required to hold down the right mouse button. This is especially handy to annotate during playback. Hitting 'Ctrl' turns off the live annotation mode and brings back the default behaviour.

# Cooperative Machine Learning