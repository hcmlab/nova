# <img src="https://github.com/hcmlab/nova/raw/master/docs/logo/nova_plain.png" alt="alt text" width="200" height="whatever">

NOVA is a tool for annotating and analyzing behaviours in social interactions.  It allows to visualize data recorded with the SSI Framework, as well as from external sources. 
A main feature of NOVA is that it allows to employ a collaborative annotation database where annotation work can be split between multiple sides,  but also between a human annotator and a machine by supporting human annotators with machine learning techniques already during the annotation process - A process we call Collaborative Machine Learning.

![alt tag](http://hcm-lab.de/projects/ssi/wp-content/uploads/2018/02/nova.png)


NOVA allows framewise labeling for a precise coding experience, and value-continuous annotations for labeling e.g emotions or social attitudes. The interface is customizable and allows loading and labeling data of multiple persons.

The Annotation format can easily be imported in other tools, like ELAN or Excel. NOVA further supports the Import of Discrete Annotation files from ELAN and ANVIL for a seamless workflow.

The Cooperative Machine Learning capabilities allow to train and evaluate machine learning models, such as Support Vector machines or Artificial neural networks directly from the interface with both, a session completion step, where a model is trained on the first minutes of an annotated sessions to predict the remaining session, and a session transfer step where a model is trained on multiple sessions to predict completly unknown data. With the help of human input the models can be refined. The collaborative ML workflow is illustrated below.

![alt tag](http://hcm-lab.de/projects/ssi/wp-content/uploads/2018/02/novacml.png)


The latest binaries can always be found [here](https://github.com/tobiasbaur/nova/releases).  

## FAQ:


<strong>Help, I opened a video but it doesn't show up</strong>

Please make sure you installed the according Video Codec on your System. E.g. The K-Lite Codec Pack might be a good solution for most missing codecs. Also make sure you installed a Visual Studio 2015 redistribution package if it doesn't come with your system.


<strong>I don't know what to do, where can I get help?</strong>

The offical documentation can be found [here](https://rawgit.com/hcmlab/nova/master/docs/index.html). If you run into trouble, please create an issue on git.


<strong>Will NOVA run on my Mac/Linux Machine?</strong>

As NOVA is an WPF Application it will run on Windows. Nevertheless you can of course use a Virtual Machine to use it. 

<strong>I found a bug, can you fix it?</strong>

NOVA is Software under development and is provided “as is”. If you run into any problems or find bugs (or want to contribute to the project) feel free to open an issue here on github.

<strong>Is there an example pipeline to automatically create annotations?</strong>

check out https://github.com/hcmlab/kinect2-record for a kinect 2 example recording pipeline.

<strong>I would like to contribute to the project</strong>

Please feel free to fork or create an issue



## Publications:

If you are using NOVA in your research please consider giving us a citation:

"eXplainable Cooperative Machine Learning with NOVA", Tobias Baur, Alexander Heimerl, Florian Lingenfelser, Johannes Wagner, Michel F. Valstar, Björn Schuller, Elisabeth André, in Ki - Künstliche Intelligenz, 2020


<pre><code>

@Article{Baur2020,
author="Baur, Tobias
and Heimerl, Alexander
and Lingenfelser, Florian
and Wagner, Johannes
and Valstar, Michel F.
and Schuller, Bj{\"o}rn
and Andr{\'e}, Elisabeth",
title="eXplainable Cooperative Machine Learning with NOVA",
journal="KI - K{\"u}nstliche Intelligenz",
year="2020",
month="Jan",
day="19",
issn="1610-1987",
doi="10.1007/s13218-020-00632-3",
url="https://doi.org/10.1007/s13218-020-00632-3"
}

</code></pre>



 “NOVA - A tool for eXplainable Cooperative Machine Learning”, Alexander Heimerl, Tobias Baur, Florian Lingenfelser, Johannes Wagner, Elisabeth André, in Proceedings of the 2019 8th International Conference on Affective Computing and Intelligent Interaction (ACII), Cambridge, September 2019
 

<pre><code>

@INPROCEEDINGS{
author={A. {Heimerl} and T. {Baur} and F. {Lingenfelser} and J. {Wagner} and E. {André}},
booktitle={2019 8th International Conference on Affective Computing and Intelligent Interaction (ACII)},
title={NOVA - A tool for eXplainable Cooperative Machine Learning},
year={2019},
volume={},
number={},
pages={109-115},
doi={10.1109/ACII.2019.8925519},
ISSN={2156-8103},
month={Sep.}
}
  
</code></pre>

