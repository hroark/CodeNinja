from django.contrib import admin
from django.urls import include, path
from Posts.views import index,detail

urlpatterns = [
    #path('', include('Posts.urls')),
    path("", index),
    path("posts", include("Posts.urls")),
    path("admin", admin.site.urls),
    path('<slug:slug>/',detail, name='detail'),
]