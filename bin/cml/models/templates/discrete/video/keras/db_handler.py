import configparser
from pymongo import MongoClient
from xml.dom import minidom



annotator_collection = 'Annotators'
scheme_collection = 'Schemes'
role_collection = 'Roles'
annotation_collection = 'Annotations'
session_collection = 'Sessions'
annotation_data_collection = 'AnnotationData'
# MetaCollection = 'Meta'


def get_docs_by_prop(vals, property, database, collection, client):
    filter = []

    if not type(vals) == type(list()):
        vals = [vals]

    for n in vals:
        filter.append({property: n})

    filter = {"$or": filter}
    ret = list(client[database][collection].find(filter))
    return ret


def get_annotation_docs(mongo_schemes, mongo_annotators, mongo_roles, database, collection, client):
    scheme_filter = []
    role_filter = []
    annotator_filter = []

    for ms in mongo_schemes:
        scheme_filter.append({'scheme_id': ms['_id']})

    for ma in mongo_annotators:
        annotator_filter.append({'annotator_id': ma['_id']})

    for mr in mongo_roles:
        role_filter.append({'role_id': mr['_id']})

    filter = {
        '$and': [
            {'$or': scheme_filter},
            {'$or': role_filter},
            {'$or': annotator_filter},
        ]
    }

    ret = list(client[database][collection].find(filter))
    return ret


def get_annos(ip, port, user, pw, corpus, scheme, annotator, roles):
    # connecting the database
    client = MongoClient(host=ip, port=port, username=user, password=pw)

    # navigating to the database collections to gather relevant documents
    databases = client.list_database_names()
    if not corpus in databases:
        print('{} not found in databases'.format(corpus))

    mongo_schemes = get_docs_by_prop(scheme, 'name', corpus, scheme_collection, client)
    if not mongo_schemes:
        print('no entries with scheme {} found'.format(scheme))
        exit()
    mongo_annotators = get_docs_by_prop(annotator, 'name', corpus, annotator_collection, client)
    if not mongo_annotators:
        print('no entries for annotator {} found'.format(scheme))
        exit()
    mongo_roles = get_docs_by_prop(roles, 'name', corpus, role_collection, client)
    if not mongo_roles:
        print('no entries for role {} found'.format(scheme))
        exit()

    mongo_annos = get_annotation_docs(mongo_schemes, mongo_annotators, mongo_roles, corpus, annotation_collection,
                                      client)

    # getting the annotation data and the session name

    data = []

    for ma in mongo_annos:
        ad = get_docs_by_prop(ma['data_id'], '_id', corpus, annotation_data_collection, client)
        s = get_docs_by_prop(ma['session_id'], '_id', corpus, session_collection, client)
        data.append((ad, s))

    return data


def get_anno_by_session(db_info, corpus_name, session_name, annotator_name, role_name):

    client = MongoClient(host=db_info["ip"], port=int(db_info["port"]), username=db_info["user"],
                         password=db_info["pw"])

    session_filter = []
    scheme_filter = []
    role_filter = []
    annotator_filter = []
    data_filter = []

    session = get_docs_by_prop(session_name, 'name', corpus_name, session_collection, client)
    scheme = get_docs_by_prop(db_info['scheme'], 'name', corpus_name, scheme_collection, client)
    annotator = get_docs_by_prop(annotator_name, 'name', corpus_name, annotator_collection, client)
    role = get_docs_by_prop(role_name, 'name', corpus_name, role_collection, client)

    if session.__len__() > 0 and scheme.__len__() > 0 and annotator.__len__() > 0 and role.__len__() > 0:

        session_filter.append({'session_id': session[0]['_id']})

        scheme_filter.append({'scheme_id': scheme[0]['_id']})

        annotator_filter.append({'annotator_id': annotator[0]['_id']})

        role_filter.append({'role_id': role[0]['_id']})

        filter = {
            '$and': [
                {'$or': session_filter},
                {'$or': scheme_filter},
                {'$or': role_filter},
                {'$or': annotator_filter},
            ]
        }

        anno = list(client[corpus_name][annotation_collection].find(filter))

        if anno.__len__() < 1:
            return None

        data_filter.append({'_id': anno[0]['data_id']})

        filter = {
            '$and': [
                {'$or': data_filter}
            ]
        }

        anno_data = list(client[corpus_name][annotation_data_collection].find(filter))

        if scheme[0]['type'] == 'DISCRETE':

            header_scheme = ""
            i = 0
            for s in scheme[0]['labels']:
                header_scheme = header_scheme + "<item name=\"" + s['name'] + "\" id=\"" + str(s['id']) + "\" />"
                i = i+1
            header_scheme = header_scheme + "<item name=\"" + "REST" + "\" id=\"" + str(i) + "\" />"

            header = ''''<?xml version="1.0" ?>
                    <annotation ssi-v="3">
                        <info ftype="ASCII" size="{}" />
                        <meta role="{}" annotator="{}" />
                        <scheme name="{}" type="{}">
                            {}
                        </scheme>
                    </annotation>
                    '''.format(anno_data.__len__(), role_name, annotator_name, db_info['scheme'], scheme[0]['type'],
                               header_scheme)
            header = header.replace('\n', '')[1:]
        else:
            # TODO continuous annotation header
            pass

        return header, anno_data

    else:
        return None

