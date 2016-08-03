# Novel Open Variable Annotator
NovA is a tool for annotating and analyzing behaviours in social interactions. It allows to load data recorded with the SSI Framework, as well as from external sources. It further makes use of SSI for semi-automated labeling of behaviours for example by automatically detecting specific gestures from a Kinect (or Kinect 2) Sensor or facial expressions from video.

![alt tag](http://hcm-lab.de/projects/ssi/wp-content/uploads/2016/07/nova.png)


NovA has been completly reworked with more advanced annotation features. It now allows framewise labeling for a more precise coding experience, and continuous annotations for labeling e.g emotions or social attitudes (see picture below). The interface is more customizable than the last version and allows loading and labeling data of multiple persons.

The Annotation format can easily be imported in other tools, like ELAN or Excel. NovA further supports the Import of Discrete Annotation files from ELAN and ANVIL for a seamless workflow. Annotations further can directly be transformed into SSI samplelists for training models.
Additionally it's now possible to store/load annotations in a local or external MongoDB database for a cooperative workflow (basic support). 

![alt tag](http://hcm-lab.de/projects/ssi/wp-content/uploads/2016/07/cont.png)

The new reworked version is now online for Download.
The latest binaries can always be found [here](https://github.com/tobiasbaur/nova/releases) 


###FAQ:

<strong>Help, I opened a video but it doesn't show up</strong>

Please make sure you installed the according Video Codec on your System. E.g. The K-Lite Codec Pack might be a good solution for most missing codecs.

<strong>Will NovA run on my Mac/Linux Machine?</strong>

As NovA is an WPF Application it will run on Windows. Nevertheless you can of course use a Virtual Machine to use it.

<strong>I found a bug, can you fix it?</strong>

NovA is  Software under development and is provided “as is”. If you run into any problems or find bugs (or want to contribute to the project) feel free to open an issue here on github.

<strong>Is there an example pipeline to automatically create annotations?</strong>

An example SSI Pipeline for recording a wide range of interessting features will be provided in one of the next updates (as it existed for the old version, but updated for Kinect 2, and some more new features 🙂 ).

<strong>I would like to contribute to the project</strong>

Please feel free to fork or create an issue



##Publications:

If you are using NovA for your research please consider giving us a citation:

 “Context-Aware Automated Analysis and Annotation of Social Human-Agent Interactions”, Tobias Baur, Gregor Mehlmann, Ionut Damian, Florian Lingenfelser, Johannes Wagner, Birgit Lugrin, Elisabeth André, Patrick Gebhard, in ACM Transactions on Interactive Intelligent Systems (TiiS) 5.2, 2015

<pre><code>

@article{
  title={Context-Aware Automated Analysis and Annotation of Social Human-Agent Interactions},
  author={Baur, Tobias and Mehlmann, Gregor and Damian, Ionut and Lingenfelser, Florian and Wagner, Johannes and Lugrin, Birgit and Andr{\'e}, Elisabeth and Gebhard, Patrick},
  journal={ACM Transactions on Interactive Intelligent Systems (TiiS)},
  volume={5},
  number={2},
  pages={11},
  year={2015},
  publisher={ACM}
}

</code></pre>
 “NovA: Automated Analysis of Nonverbal Signals in Social Interactions” Tobias Baur, Ionut Damian, Florian Lingenfelser, Johannes Wagner and Elisabeth André, in Human Behavior Understanding, LNCS 8212, 2013.

<pre><code>
@incollection{
year={2013},
isbn={978-3-319-02713-5},
booktitle={Human Behavior Understanding},
volume={8212},
series={Lecture Notes in Computer Science},
editor={Salah, Albert Ali and Hung, Hayley and Aran, Oya and Gunes, Hatice},
doi={10.1007/978-3-319-02714-2_14},
title={NovA: Automated Analysis of Nonverbal Signals in Social Interactions},
url={http://dx.doi.org/10.1007/978-3-319-02714-2_14},
publisher={Springer International Publishing},
author={Baur, Tobias and Damian, Ionut and Lingenfelser, Florian and Wagner, Johannes and André, Elisabeth},
pages={160-171}}

</code></pre>