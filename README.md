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


 “Show Me What You’ve Learned: Applying Cooperative Machine Learning for the Semi-Automated Annotation of Social Signals”, Johannes Wagner, Tobias Baur, Dominik Schiller, Yue Zhang, Björn Schuller, Michel F. Valstar, Elisabeth André in Proceedings of the 27th International Joint Conference on Artificial Intelligence (IJCAI) and the 23rd European Conference on Artificial Intelligence (ECAI), Workshop on Explainable Artificial Intelligence July 13-19, 2018, Stockholm, Sweden 
 

<pre><code>

@article{
  title={Show Me What You’ve Learned: Applying Cooperative Machine Learning for the Semi-Automated Annotation of Social Signals},
  author={Wagner, Johannes and Baur, Tobias and Schiller, Dominik and Zhang, Yue and Schuller, Bj{\"o}rn and Valstar, Michel and Andr{\'e}, Elisabeth},
  journal={Proceedings of the 27th International Joint Conference on Artificial Intelligence (IJCAI) and the 23rd European Conference on Artificial Intelligence (ECAI), Workshop on Explainable Artificial Intelligence, Stockholm, Sweden},
  pages={171},
  year={2018}
}
  
</code></pre>



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
