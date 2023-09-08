from django.contrib import admin
from django.urls import include, path
from posts.views import index,detail

urlpatterns = [
    #path('', include('posts.urls')),
    path("", index),
    path("posts", include("posts.urls")),
    path("admin", admin.site.urls),
    path('<slug:slug>/',detail, name='detail'),
]