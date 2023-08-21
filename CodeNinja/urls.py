from django.contrib import admin
from django.urls import include, path
from django.conf.urls.static import static
from django.urls.resolvers import settings 

urlpatterns = [
    path('', include('Posts.urls')),
    path("posts", include("Posts.urls")),
    path("admin", admin.site.urls),
]