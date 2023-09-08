from django.shortcuts import render
from posts.models import basePost, requestPost,codePost
from django.contrib.auth.forms import UserCreationForm

def index(request):
    requestList = requestPost.objects.order_by("createDate")[:5]
    ctx={"requestList":requestList}
    return render(request,'posts/index.html/',ctx)

def detail (request,slug):
    post = requestPost.objects.get(slug=slug)
    return render(request,'posts/detail.html', {'post':post})
